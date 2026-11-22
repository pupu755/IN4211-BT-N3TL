using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Domain;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static (Restaurant Restaurant, MenuItem Pho, MenuItem Tea) CreateRestaurant()
    {
        var restaurant = Restaurant.Create(Guid.NewGuid(), "Pho 24", "1 Le Loi");
        var pho = restaurant.AddMenuItem("Pho", Money.Of(50000m));
        var tea = restaurant.AddMenuItem("Tea", Money.Of(10000m));
        return (restaurant, pho, tea);
    }

    private static Order CreateOrder(Restaurant restaurant) =>
        Order.Create(Guid.NewGuid(), restaurant.Id, Now);

    private static Order CreateConfirmedOrder()
    {
        var (restaurant, pho, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);
        order.AddItem(pho, 1);
        order.Confirm();
        return order;
    }

    // QT01

    [Fact]
    public void AddItem_FromDifferentRestaurant_ThrowsDomainException() // QT01
    {
        var (restaurantA, phoA, _) = CreateRestaurant();
        var restaurantB = Restaurant.Create(Guid.NewGuid(), "Com Tam", "2 Nguyen Hue");
        var rice = restaurantB.AddMenuItem("Rice", Money.Of(40000m));
        var order = CreateOrder(restaurantA);
        order.AddItem(phoA, 1);

        Assert.Throws<DomainException>(() => order.AddItem(rice, 1));
    }

    [Fact]
    public void AddItem_FromSameRestaurant_AddsItems() // QT01
    {
        var (restaurant, pho, tea) = CreateRestaurant();
        var order = CreateOrder(restaurant);

        order.AddItem(pho, 1);
        order.AddItem(tea, 2);

        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void AddItem_SameDishTwice_MergesQuantity()
    {
        var (restaurant, pho, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);

        order.AddItem(pho, 1);
        order.AddItem(pho, 2);

        Assert.Equal(3, Assert.Single(order.Items).Quantity);
    }

    [Fact]
    public void AddItem_ZeroQuantity_ThrowsDomainException()
    {
        var (restaurant, pho, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);

        Assert.Throws<DomainException>(() => order.AddItem(pho, 0));
    }

    [Fact]
    public void AddItem_UnavailableDish_ThrowsDomainException()
    {
        var (restaurant, pho, _) = CreateRestaurant();
        pho.SetAvailability(false);
        var order = CreateOrder(restaurant);

        Assert.Throws<DomainException>(() => order.AddItem(pho, 1));
    }

    [Fact]
    public void AddItem_AfterConfirm_ThrowsDomainException()
    {
        var (restaurant, pho, tea) = CreateRestaurant();
        var order = CreateOrder(restaurant);
        order.AddItem(pho, 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.AddItem(tea, 1));
    }

    // QT05

    [Fact]
    public void Total_MultipleItems_EqualsSumOfPriceTimesQuantity() // QT05
    {
        var (restaurant, pho, tea) = CreateRestaurant();
        var order = CreateOrder(restaurant);

        order.AddItem(pho, 2);
        order.AddItem(tea, 3);

        Assert.Equal(130000m, order.Total.Amount);
    }

    [Fact]
    public void Total_EmptyOrder_IsZero() // QT05
    {
        var (restaurant, _, _) = CreateRestaurant();

        Assert.Equal(0m, CreateOrder(restaurant).Total.Amount);
    }

    [Fact]
    public void Total_AfterMenuPriceChange_KeepsPriceAtOrderTime() // QT05
    {
        var (restaurant, pho, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);
        order.AddItem(pho, 1);

        pho.ChangePrice(Money.Of(99000m));

        Assert.Equal(50000m, order.Total.Amount);
    }

    // QT04

    [Fact]
    public void Create_NewOrder_StartsPending() // QT04
    {
        var (restaurant, _, _) = CreateRestaurant();

        Assert.Equal(OrderStatus.Pending, CreateOrder(restaurant).Status);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException() // QT04
    {
        var (restaurant, _, _) = CreateRestaurant();

        Assert.Throws<DomainException>(() => CreateOrder(restaurant).Confirm());
    }

    [Fact]
    public void Advance_FullHappyPath_ReachesDelivered() // QT04
    {
        var order = CreateConfirmedOrder();

        order.StartPreparing();
        order.MarkReadyForPickup();
        order.StartDelivering();
        order.MarkDelivered();

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void StartPreparing_FromPending_ThrowsDomainException() // QT04
    {
        var (restaurant, pho, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);
        order.AddItem(pho, 1);

        Assert.Throws<DomainException>(() => order.StartPreparing());
    }

    [Fact]
    public void StartDelivering_FromPreparing_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();
        order.StartPreparing();

        Assert.Throws<DomainException>(() => order.StartDelivering());
    }

    [Fact]
    public void MarkDelivered_FromConfirmed_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();

        Assert.Throws<DomainException>(() => order.MarkDelivered());
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();

        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Cancel_FromPending_SetsCancelled() // QT04
    {
        var (restaurant, _, _) = CreateRestaurant();
        var order = CreateOrder(restaurant);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsCancelled() // QT04
    {
        var order = CreateConfirmedOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_FromPreparing_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();
        order.StartPreparing();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Cancel_FromDelivering_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();
        order.StartPreparing();
        order.MarkReadyForPickup();
        order.StartDelivering();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Advance_AfterCancelled_ThrowsDomainException() // QT04
    {
        var order = CreateConfirmedOrder();
        order.Cancel();

        Assert.Throws<DomainException>(() => order.StartPreparing());
    }
}
