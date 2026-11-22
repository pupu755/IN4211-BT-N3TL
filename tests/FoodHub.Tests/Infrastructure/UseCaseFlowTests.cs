using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Services;
using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Tests.Infrastructure;

/// <summary>Runs the real use cases against SQLite to check the rules end to end at service level.</summary>
public class UseCaseFlowTests
{
    private sealed record Actors(AuthResponse Customer, AuthResponse Owner, AuthResponse Shipper, RestaurantDto Restaurant);

    private static async Task<Actors> SeedAsync(IServiceProvider sp, string suffix = "")
    {
        var auth = sp.GetRequiredService<AuthService>();
        var customer = await auth.RegisterAsync(new RegisterRequest($"cus{suffix}@x.com", "password123", "Cus", Role.Customer, null));
        var owner = await auth.RegisterAsync(new RegisterRequest($"own{suffix}@x.com", "password123", "Owner", Role.Restaurant, null));
        var shipper = await auth.RegisterAsync(new RegisterRequest($"shp{suffix}@x.com", "password123", "Ship", Role.Shipper, "59A1-00001"));
        var restaurants = sp.GetRequiredService<RestaurantService>();
        var restaurant = await restaurants.CreateAsync(owner.UserId, new CreateRestaurantRequest($"Pho {suffix}", "1 Le Loi"));
        await restaurants.AddMenuItemAsync(owner.UserId, restaurant.Id, new AddMenuItemRequest("Pho", 50000m));
        await restaurants.AddMenuItemAsync(owner.UserId, restaurant.Id, new AddMenuItemRequest("Tea", 10000m));
        restaurant = await restaurants.GetAsync(restaurant.Id);
        return new Actors(customer, owner, shipper, restaurant);
    }

    private static async Task<OrderDto> ReadyOrderAsync(IServiceProvider sp, Actors a)
    {
        var orders = sp.GetRequiredService<OrderService>();
        var order = await orders.CreateAsync(a.Customer.UserId, new CreateOrderRequest(
            a.Restaurant.Id, new[] { new OrderLineRequest(a.Restaurant.MenuItems[0].Id, 1) }));
        await orders.ConfirmAsync(a.Owner.UserId, order.Id);
        await orders.StartPreparingAsync(a.Owner.UserId, order.Id);
        return await orders.MarkReadyAsync(a.Owner.UserId, order.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        var request = new RegisterRequest("dup@x.com", "password123", "A", Role.Customer, null);
        await auth.RegisterAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => auth.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_AdminRole_ThrowsForbidden()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            auth.RegisterAsync(new RegisterRequest("adm@x.com", "password123", "A", Role.Admin, null)));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();
        await auth.RegisterAsync(new RegisterRequest("a@x.com", "password123", "A", Role.Customer, null));

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => auth.LoginAsync(new LoginRequest("a@x.com", "wrongpass1")));
    }

    [Fact]
    public async Task Refresh_UsedTwice_SecondCallIsRejected()
    {
        using var host = new TestHost();
        AuthResponse first;
        using (var scope = host.NewScope())
        {
            first = await scope.ServiceProvider.GetRequiredService<AuthService>()
                .RegisterAsync(new RegisterRequest("a@x.com", "password123", "A", Role.Customer, null));
        }

        using (var scope = host.NewScope())
        {
            var renewed = await scope.ServiceProvider.GetRequiredService<AuthService>()
                .RefreshAsync(new RefreshRequest(first.RefreshToken));
            Assert.NotEqual(first.RefreshToken, renewed.RefreshToken);
        }

        using (var scope = host.NewScope())
        {
            await Assert.ThrowsAsync<UnauthorizedAppException>(() => scope.ServiceProvider
                .GetRequiredService<AuthService>().RefreshAsync(new RefreshRequest(first.RefreshToken)));
        }
    }

    [Fact]
    public async Task CreateOrder_ItemsFromTwoRestaurants_ThrowsDomainException() // QT01
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var restaurants = sp.GetRequiredService<RestaurantService>();
        var other = await restaurants.CreateAsync(a.Owner.UserId, new CreateRestaurantRequest("Com Tam", "2 Nguyen Hue"));
        var rice = await restaurants.AddMenuItemAsync(a.Owner.UserId, other.Id, new AddMenuItemRequest("Rice", 40000m));

        var request = new CreateOrderRequest(a.Restaurant.Id, new[]
        {
            new OrderLineRequest(a.Restaurant.MenuItems[0].Id, 1),
            new OrderLineRequest(rice.Id, 1)
        });

        await Assert.ThrowsAsync<DomainException>(() => sp.GetRequiredService<OrderService>().CreateAsync(a.Customer.UserId, request));
    }

    [Fact]
    public async Task CreateOrder_ReturnsTotalFeeAndGrandTotal() // QT05
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");

        var order = await sp.GetRequiredService<OrderService>().CreateAsync(a.Customer.UserId, new CreateOrderRequest(
            a.Restaurant.Id, new[]
            {
                new OrderLineRequest(a.Restaurant.MenuItems.First(m => m.Name == "Pho").Id, 2),
                new OrderLineRequest(a.Restaurant.MenuItems.First(m => m.Name == "Tea").Id, 3)
            }));

        Assert.Equal(130000m, order.Subtotal);
        Assert.Equal(15000m, order.DeliveryFee);
        Assert.Equal(145000m, order.GrandTotal);
    }

    [Fact]
    public async Task Confirm_ByOtherRestaurantOwner_ThrowsForbidden()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var stranger = await sp.GetRequiredService<AuthService>()
            .RegisterAsync(new RegisterRequest("str@x.com", "password123", "S", Role.Restaurant, null));
        var order = await sp.GetRequiredService<OrderService>().CreateAsync(a.Customer.UserId, new CreateOrderRequest(
            a.Restaurant.Id, new[] { new OrderLineRequest(a.Restaurant.MenuItems[0].Id, 1) }));

        await Assert.ThrowsAsync<ForbiddenException>(() => sp.GetRequiredService<OrderService>().ConfirmAsync(stranger.UserId, order.Id));
    }

    [Fact]
    public async Task StartPreparing_BeforeConfirm_ThrowsDomainException() // QT04
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var orders = sp.GetRequiredService<OrderService>();
        var order = await orders.CreateAsync(a.Customer.UserId, new CreateOrderRequest(
            a.Restaurant.Id, new[] { new OrderLineRequest(a.Restaurant.MenuItems[0].Id, 1) }));

        await Assert.ThrowsAsync<DomainException>(() => orders.StartPreparingAsync(a.Owner.UserId, order.Id));
    }

    [Fact]
    public async Task Cancel_AfterPreparing_ThrowsDomainException() // QT04
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var ready = await ReadyOrderAsync(sp, a);

        await Assert.ThrowsAsync<DomainException>(() =>
            sp.GetRequiredService<OrderService>().CancelAsync(a.Customer.UserId, Role.Customer, ready.Id));
    }

    [Fact]
    public async Task FullDeliveryFlow_WithCorrectCode_EndsDelivered() // QT03 QT04
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var ready = await ReadyOrderAsync(sp, a);
        var delivery = sp.GetRequiredService<DeliveryService>();

        Assert.Single(await delivery.ListAvailableAsync());
        await delivery.AcceptAsync(a.Shipper.UserId, ready.Id);
        Assert.Empty(await delivery.ListAvailableAsync());
        await delivery.StartDeliveringAsync(a.Shipper.UserId, ready.Id);
        var code = await delivery.GetCodeForCustomerAsync(a.Customer.UserId, ready.Id);
        await delivery.ConfirmReceiptAsync(a.Shipper.UserId, ready.Id, new ConfirmReceiptRequest(code.Code));

        var final = await sp.GetRequiredService<OrderService>().GetAsync(a.Customer.UserId, Role.Customer, ready.Id);
        Assert.Equal(OrderStatus.Delivered, final.Status);
    }

    [Fact]
    public async Task GetCode_ByAnotherCustomer_ThrowsForbidden() // QT03
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var ready = await ReadyOrderAsync(sp, a);
        var delivery = sp.GetRequiredService<DeliveryService>();
        await delivery.AcceptAsync(a.Shipper.UserId, ready.Id);
        await delivery.StartDeliveringAsync(a.Shipper.UserId, ready.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => delivery.GetCodeForCustomerAsync(Guid.NewGuid(), ready.Id));
    }

    [Fact]
    public async Task ConfirmReceipt_WrongCodeFiveTimes_LocksAndRejectsCorrectCode() // QT03
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var ready = await ReadyOrderAsync(sp, a);
        var delivery = sp.GetRequiredService<DeliveryService>();
        await delivery.AcceptAsync(a.Shipper.UserId, ready.Id);
        await delivery.StartDeliveringAsync(a.Shipper.UserId, ready.Id);
        var code = (await delivery.GetCodeForCustomerAsync(a.Customer.UserId, ready.Id)).Code;
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<RequestValidationException>(() =>
                delivery.ConfirmReceiptAsync(a.Shipper.UserId, ready.Id, new ConfirmReceiptRequest(wrong)));
        }

        await Assert.ThrowsAsync<ConflictException>(() =>
            delivery.ConfirmReceiptAsync(a.Shipper.UserId, ready.Id, new ConfirmReceiptRequest(code)));
    }

    [Fact]
    public async Task Accept_ThirdUndeliveredOrder_ThrowsDomainException() // QT02
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var delivery = sp.GetRequiredService<DeliveryService>();

        for (var i = 0; i < 2; i++)
        {
            await delivery.AcceptAsync(a.Shipper.UserId, (await ReadyOrderAsync(sp, a)).Id);
        }

        var third = await ReadyOrderAsync(sp, a);
        await Assert.ThrowsAsync<DomainException>(() => delivery.AcceptAsync(a.Shipper.UserId, third.Id));
    }

    [Fact]
    public async Task Accept_OrderNotReady_ThrowsConflict()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var order = await sp.GetRequiredService<OrderService>().CreateAsync(a.Customer.UserId, new CreateOrderRequest(
            a.Restaurant.Id, new[] { new OrderLineRequest(a.Restaurant.MenuItems[0].Id, 1) }));

        await Assert.ThrowsAsync<ConflictException>(() => sp.GetRequiredService<DeliveryService>().AcceptAsync(a.Shipper.UserId, order.Id));
    }

    [Fact]
    public async Task Accept_OrderAlreadyAcceptedByAnotherShipper_ThrowsConflict()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var sp = scope.ServiceProvider;
        var a = await SeedAsync(sp, "1");
        var second = await sp.GetRequiredService<AuthService>()
            .RegisterAsync(new RegisterRequest("shp2@x.com", "password123", "Ship2", Role.Shipper, "59A1-00002"));
        var ready = await ReadyOrderAsync(sp, a);
        var delivery = sp.GetRequiredService<DeliveryService>();
        await delivery.AcceptAsync(a.Shipper.UserId, ready.Id);

        await Assert.ThrowsAsync<ConflictException>(() => delivery.AcceptAsync(second.UserId, ready.Id));
    }
}
