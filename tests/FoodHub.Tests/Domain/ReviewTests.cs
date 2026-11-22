using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Domain;

public class ReviewTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);

    private static (Order Order, Guid CustomerId) CreateDeliveredOrder()
    {
        var customerId = Guid.NewGuid();
        var restaurant = Restaurant.Create(Guid.NewGuid(), "Pho 24", "1 Le Loi");
        var pho = restaurant.AddMenuItem("Pho", Money.Of(50000m));
        var order = Order.Create(customerId, restaurant.Id, Now);
        order.AddItem(pho, 1);
        order.Confirm();
        order.StartPreparing();
        order.MarkReadyForPickup();
        order.StartDelivering();
        order.MarkDelivered();
        return (order, customerId);
    }

    [Fact]
    public void Create_DeliveredOrder_CreatesReviewSuccessfully()
    {
        var (order, customerId) = CreateDeliveredOrder();

        var review = Review.Create(order, customerId, 5, "Ngon tuyet voi", Now);

        Assert.Equal(order.Id, review.OrderId);
        Assert.Equal(customerId, review.CustomerId);
        Assert.Equal(order.RestaurantId, review.RestaurantId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Ngon tuyet voi", review.Comment);
    }

    [Fact]
    public void Create_NonDeliveredOrder_ThrowsDomainException()
    {
        var customerId = Guid.NewGuid();
        var order = Order.Create(customerId, Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() => Review.Create(order, customerId, 5, "Good", Now));
    }

    [Fact]
    public void Create_ByDifferentCustomer_ThrowsDomainException()
    {
        var (order, _) = CreateDeliveredOrder();

        Assert.Throws<DomainException>(() => Review.Create(order, Guid.NewGuid(), 5, "Good", Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_InvalidRating_ThrowsDomainException(int rating)
    {
        var (order, customerId) = CreateDeliveredOrder();

        Assert.Throws<DomainException>(() => Review.Create(order, customerId, rating, "Good", Now));
    }

    [Fact]
    public void Create_CommentExceeds500Chars_ThrowsDomainException()
    {
        var (order, customerId) = CreateDeliveredOrder();
        var longComment = new string('a', 501);

        Assert.Throws<DomainException>(() => Review.Create(order, customerId, 5, longComment, Now));
    }
}
