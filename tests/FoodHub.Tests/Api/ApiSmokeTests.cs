using System.Net;
using System.Net.Http.Json;
using FoodHub.API.Middleware;
using FoodHub.Application.Dtos;
using FoodHub.Domain.Enums;

namespace FoodHub.Tests.Api;

public class ApiSmokeTests : IClassFixture<FoodHubApiFactory>
{
    private readonly FoodHubApiFactory _factory;

    public ApiSmokeTests(FoodHubApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerJson_DeclaresBearerSecurityScheme()
    {
        var json = await _factory.CreateClient().GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("\"securitySchemes\"", json);
        Assert.Contains("\"bearer\"", json);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401Json()
    {
        var response = await _factory.CreateClient().GetAsync("/api/orders/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("unauthorized", error!.Code);
    }

    [Fact]
    public async Task CustomerEndpoint_WithShipperToken_Returns403Json()
    {
        var client = await _factory.CreateClientForAsync("shipper@foodhub.local");

        var response = await client.GetAsync("/api/orders/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("forbidden", error!.Code);
    }

    [Fact]
    public async Task Login_SeededAccountOfEachRole_ReturnsToken()
    {
        foreach (var email in new[] { "admin@foodhub.local", "customer@foodhub.local", "restaurant@foodhub.local", "shipper@foodhub.local" })
        {
            var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, FoodHubApiFactory.SeedPassword));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest("customer@foodhub.local", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidBody_Returns422WithErrors()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("not-an-email", "short", "", Role.Customer, null), Json.Options);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.NotEmpty(error!.Errors!);
    }

    [Fact]
    public async Task Register_ExistingEmail_Returns409()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("customer@foodhub.local", "password123", "Dup", Role.Customer, null), Json.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetRestaurant_UnknownId_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/restaurants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListRestaurants_Anonymous_ReturnsSeededRestaurant()
    {
        var list = await _factory.CreateClient().GetFromJsonAsync<List<RestaurantDto>>("/api/restaurants", Json.Options);

        Assert.Contains(list!, r => r.Name == "Sample Pho 24" && r.MenuItems.Count == 2);
    }

    [Fact]
    public async Task FullOrderAndDeliveryFlow_ViaHttp_CompletesSuccessfully()
    {
        var anonClient = _factory.CreateClient();
        var restaurants = await anonClient.GetFromJsonAsync<List<RestaurantDto>>("/api/restaurants", Json.Options);
        var restaurant = Assert.Single(restaurants!);
        var menuItem = restaurant.MenuItems.First(m => m.Name == "Pho bo");

        var customerClient = await _factory.CreateClientForAsync("customer@foodhub.local");
        var createOrderRes = await customerClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            restaurant.Id, new[] { new OrderLineRequest(menuItem.Id, 2) }), Json.Options);
        Assert.Equal(HttpStatusCode.Created, createOrderRes.StatusCode);
        var order = await createOrderRes.Content.ReadFromJsonAsync<OrderDto>(Json.Options);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order!.Status);

        var restaurantClient = await _factory.CreateClientForAsync("restaurant@foodhub.local");
        var confirmRes = await restaurantClient.PostAsync($"/api/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        var prepRes = await restaurantClient.PostAsync($"/api/orders/{order.Id}/prepare", null);
        Assert.Equal(HttpStatusCode.OK, prepRes.StatusCode);

        var readyRes = await restaurantClient.PostAsync($"/api/orders/{order.Id}/ready", null);
        Assert.Equal(HttpStatusCode.OK, readyRes.StatusCode);

        var shipperClient = await _factory.CreateClientForAsync("shipper@foodhub.local");
        var available = await shipperClient.GetFromJsonAsync<List<OrderDto>>("/api/deliveries/available", Json.Options);
        Assert.Contains(available!, o => o.Id == order.Id);

        var acceptRes = await shipperClient.PostAsync($"/api/deliveries/{order.Id}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptRes.StatusCode);

        var startRes = await shipperClient.PostAsync($"/api/deliveries/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startRes.StatusCode);

        var codeRes = await customerClient.GetAsync($"/api/orders/{order.Id}/delivery-code");
        Assert.Equal(HttpStatusCode.OK, codeRes.StatusCode);
        var codeDto = await codeRes.Content.ReadFromJsonAsync<DeliveryCodeDto>(Json.Options);
        Assert.NotNull(codeDto);
        Assert.Equal(6, codeDto!.Code.Length);

        var confirmReceiptRes = await shipperClient.PostAsJsonAsync($"/api/deliveries/{order.Id}/confirm-receipt",
            new ConfirmReceiptRequest(codeDto.Code), Json.Options);
        Assert.Equal(HttpStatusCode.OK, confirmReceiptRes.StatusCode);

        var reviewRes = await customerClient.PostAsJsonAsync($"/api/reviews/orders/{order.Id}",
            new CreateReviewRequest(5, "Tuyet voi qua!"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, reviewRes.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ViaHttp_CustomerCancelsPending()
    {
        var anonClient = _factory.CreateClient();
        var restaurants = await anonClient.GetFromJsonAsync<List<RestaurantDto>>("/api/restaurants", Json.Options);
        var restaurant = Assert.Single(restaurants!);
        var menuItem = restaurant.MenuItems.First();

        var customerClient = await _factory.CreateClientForAsync("customer@foodhub.local");
        var createOrderRes = await customerClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            restaurant.Id, new[] { new OrderLineRequest(menuItem.Id, 1) }), Json.Options);
        var order = await createOrderRes.Content.ReadFromJsonAsync<OrderDto>(Json.Options);

        var cancelRes = await customerClient.PostAsync($"/api/orders/{order!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelled = await cancelRes.Content.ReadFromJsonAsync<OrderDto>(Json.Options);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }
}
