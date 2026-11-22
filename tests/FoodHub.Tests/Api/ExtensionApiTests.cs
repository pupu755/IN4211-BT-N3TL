using System.Net;
using System.Net.Http.Json;
using FoodHub.Application.Dtos;

namespace FoodHub.Tests.Api;

public class ExtensionApiTests : IClassFixture<FoodHubApiFactory>
{
    private readonly FoodHubApiFactory _factory;

    public ExtensionApiTests(FoodHubApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateCoupon_Admin_ReturnsOk()
    {
        var client = await _factory.CreateClientForAsync("admin@foodhub.local");

        var response = await client.PostAsJsonAsync("/api/coupons", new CreateCouponRequest(
            "TESTADMIN20", 20000m, 100000m, DateTime.UtcNow.AddDays(30)), Json.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var coupon = await response.Content.ReadFromJsonAsync<CouponDto>(Json.Options);
        Assert.Equal("TESTADMIN20", coupon!.Code);
    }

    [Fact]
    public async Task CreateCoupon_CustomerRole_ReturnsForbidden()
    {
        var client = await _factory.CreateClientForAsync("customer@foodhub.local");

        var response = await client.PostAsJsonAsync("/api/coupons", new CreateCouponRequest(
            "TESTCUS", 20000m, 100000m, DateTime.UtcNow.AddDays(30)), Json.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRestaurantStatistics_Owner_ReturnsOk()
    {
        var client = await _factory.CreateClientForAsync("restaurant@foodhub.local");
        var restaurants = await client.GetFromJsonAsync<List<RestaurantDto>>("/api/restaurants/mine", Json.Options);
        var restaurant = Assert.Single(restaurants!);

        var response = await client.GetAsync($"/api/restaurants/{restaurant.Id}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<RestaurantRevenueDto>(Json.Options);
        Assert.NotNull(stats);
        Assert.Equal(restaurant.Id, stats!.RestaurantId);
    }

    [Fact]
    public async Task GetRestaurantStatistics_Customer_ReturnsForbidden()
    {
        var client = await _factory.CreateClientForAsync("customer@foodhub.local");

        var response = await client.GetAsync($"/api/restaurants/{Guid.NewGuid()}/statistics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReviews_Anonymous_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/reviews/restaurants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
