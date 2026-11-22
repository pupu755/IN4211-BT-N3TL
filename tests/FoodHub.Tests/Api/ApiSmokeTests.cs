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
}
