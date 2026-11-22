using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Services;

internal static class Mapper
{
    public static MenuItemDto ToDto(this MenuItem item) =>
        new(item.Id, item.Name, item.Price.Amount, item.IsAvailable);

    public static RestaurantDto ToDto(this Restaurant restaurant) =>
        new(restaurant.Id, restaurant.Name, restaurant.Address, restaurant.MenuItems.Select(m => m.ToDto()).ToList());

    public static OrderDto ToDto(this Order order, IDeliveryFeeStrategy feeStrategy)
    {
        var fee = feeStrategy.Calculate(order.FinalAmount);
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.RestaurantId,
            order.Status,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount)).ToList(),
            order.Total.Amount,
            order.CouponCode,
            order.DiscountAmount.Amount,
            fee.Amount,
            order.FinalAmount.Add(fee).Amount);
    }

    public static DeliveryDto ToDto(this Delivery delivery) =>
        new(delivery.Id, delivery.OrderId, delivery.AcceptedAtUtc, delivery.StartedAtUtc, delivery.DeliveredAtUtc);

    public static ReviewDto ToDto(this Review review) =>
        new(review.Id, review.OrderId, review.CustomerId, review.RestaurantId, review.Rating, review.Comment, review.CreatedAtUtc);

    public static CouponDto ToDto(this Coupon coupon) =>
        new(coupon.Id, coupon.Code, coupon.DiscountAmount.Amount, coupon.MinOrderAmount.Amount, coupon.ExpiresAtUtc, coupon.IsActive);
}
