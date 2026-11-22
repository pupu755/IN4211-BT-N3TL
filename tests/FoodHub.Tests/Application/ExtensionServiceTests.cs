using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Services;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;
using FoodHub.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Tests.Application;

public class ExtensionServiceTests
{
    private static async Task<(Guid CustomerId, Guid OwnerId, Guid RestaurantId, Guid OrderId)> SetupDeliveredOrderAsync(IServiceProvider sp)
    {
        var auth = sp.GetRequiredService<AuthService>();
        var customer = await auth.RegisterAsync(new RegisterRequest($"cus_{Guid.NewGuid()}@x.com", "password123", "Cus", Role.Customer, null));
        var owner = await auth.RegisterAsync(new RegisterRequest($"own_{Guid.NewGuid()}@x.com", "password123", "Owner", Role.Restaurant, null));
        var shipper = await auth.RegisterAsync(new RegisterRequest($"shp_{Guid.NewGuid()}@x.com", "password123", "Ship", Role.Shipper, "59A1-99999"));

        var restaurants = sp.GetRequiredService<RestaurantService>();
        var restaurant = await restaurants.CreateAsync(owner.UserId, new CreateRestaurantRequest("Com Nieu", "10 Nguyen Trai"));
        var dish = await restaurants.AddMenuItemAsync(owner.UserId, restaurant.Id, new AddMenuItemRequest("Com nieu ca kho", 75000m));

        var orders = sp.GetRequiredService<OrderService>();
        var order = await orders.CreateAsync(customer.UserId, new CreateOrderRequest(
            restaurant.Id, new[] { new OrderLineRequest(dish.Id, 2) }));

        await orders.ConfirmAsync(owner.UserId, order.Id);
        await orders.StartPreparingAsync(owner.UserId, order.Id);
        await orders.MarkReadyAsync(owner.UserId, order.Id);

        var delivery = sp.GetRequiredService<DeliveryService>();
        await delivery.AcceptAsync(shipper.UserId, order.Id);
        await delivery.StartDeliveringAsync(shipper.UserId, order.Id);
        var code = (await delivery.GetCodeForCustomerAsync(customer.UserId, order.Id)).Code;
        await delivery.ConfirmReceiptAsync(shipper.UserId, order.Id, new ConfirmReceiptRequest(code));

        return (customer.UserId, owner.UserId, restaurant.Id, order.Id);
    }

    [Fact]
    public async Task Review_CreateForDeliveredOrder_Succeeds()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var (customerId, _, restaurantId, orderId) = await SetupDeliveredOrderAsync(sp);

        var reviews = sp.GetRequiredService<ReviewService>();
        var result = await reviews.CreateAsync(customerId, orderId, new CreateReviewRequest(5, "Com rat ngon, ca dam da!"));

        Assert.Equal(5, result.Rating);
        Assert.Equal("Com rat ngon, ca dam da!", result.Comment);
        Assert.Equal(orderId, result.OrderId);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(restaurantId, result.RestaurantId);

        var list = await reviews.ListByRestaurantAsync(restaurantId);
        Assert.Single(list);
    }

    [Fact]
    public async Task Review_CreateTwiceForSameOrder_ThrowsConflict()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var (customerId, _, _, orderId) = await SetupDeliveredOrderAsync(sp);

        var reviews = sp.GetRequiredService<ReviewService>();
        await reviews.CreateAsync(customerId, orderId, new CreateReviewRequest(5, "Lan 1"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            reviews.CreateAsync(customerId, orderId, new CreateReviewRequest(4, "Lan 2")));
    }

    [Fact]
    public async Task Coupon_CreateAndApply_ReducesGrandTotal()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var auth = sp.GetRequiredService<AuthService>();
        var customer = await auth.RegisterAsync(new RegisterRequest($"cus_{Guid.NewGuid()}@x.com", "password123", "Cus", Role.Customer, null));
        var owner = await auth.RegisterAsync(new RegisterRequest($"own_{Guid.NewGuid()}@x.com", "password123", "Owner", Role.Restaurant, null));

        var restaurants = sp.GetRequiredService<RestaurantService>();
        var restaurant = await restaurants.CreateAsync(owner.UserId, new CreateRestaurantRequest("Banh Mi", "12 Hai Ba Trung"));
        var item = await restaurants.AddMenuItemAsync(owner.UserId, restaurant.Id, new AddMenuItemRequest("Banh mi thit", 30000m));

        var coupons = sp.GetRequiredService<CouponService>();
        await coupons.CreateAsync(new CreateCouponRequest("BANHMI10K", 10000m, 50000m, DateTime.UtcNow.AddDays(7)));

        var orders = sp.GetRequiredService<OrderService>();
        var order = await orders.CreateAsync(customer.UserId, new CreateOrderRequest(
            restaurant.Id, new[] { new OrderLineRequest(item.Id, 2) })); // 60,000 subtotal

        var applied = await orders.ApplyCouponAsync(customer.UserId, order.Id, new ApplyCouponRequest("BANHMI10K"));
        Assert.Equal("BANHMI10K", applied.CouponCode);
        Assert.Equal(10000m, applied.DiscountAmount);
        Assert.Equal(60000m, applied.Subtotal);
        Assert.Equal(50000m + applied.DeliveryFee, applied.GrandTotal);

        var removed = await orders.RemoveCouponAsync(customer.UserId, order.Id);
        Assert.Null(removed.CouponCode);
        Assert.Equal(0m, removed.DiscountAmount);
        Assert.Equal(60000m + removed.DeliveryFee, removed.GrandTotal);
    }

    [Fact]
    public async Task Coupon_CreateDuplicateCode_ThrowsConflict()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var coupons = sp.GetRequiredService<CouponService>();
        await coupons.CreateAsync(new CreateCouponRequest("DUPCODE", 10000m, 50000m, DateTime.UtcNow.AddDays(7)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            coupons.CreateAsync(new CreateCouponRequest("dupcode", 15000m, 60000m, DateTime.UtcNow.AddDays(7))));
    }

    [Fact]
    public async Task RevenueStatistics_DeliveredOrders_CalculatesCorrectTotals()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var (customerId, ownerId, restaurantId, orderId) = await SetupDeliveredOrderAsync(sp);

        var restaurants = sp.GetRequiredService<RestaurantService>();
        var stats = await restaurants.GetRevenueStatisticsAsync(ownerId, restaurantId, null, null);

        Assert.Equal(1, stats.TotalDeliveredOrders);
        Assert.Equal(150000m, stats.TotalRevenue); // 2 x 75,000
        Assert.Equal(0, stats.CancelledOrders);
        Assert.Single(stats.TopSellingItems);
        Assert.Equal("Com nieu ca kho", stats.TopSellingItems[0].Name);
        Assert.Equal(2, stats.TopSellingItems[0].QuantitySold);
    }

    [Fact]
    public async Task RevenueStatistics_ByStranger_ThrowsForbidden()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;

        var (_, _, restaurantId, _) = await SetupDeliveredOrderAsync(sp);

        var restaurants = sp.GetRequiredService<RestaurantService>();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            restaurants.GetRevenueStatisticsAsync(Guid.NewGuid(), restaurantId, null, null));
    }
}
