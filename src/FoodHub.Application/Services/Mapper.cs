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
        var fee = feeStrategy.Calculate(order.Total);
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.RestaurantId,
            order.Status,
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDto(i.MenuItemId, i.Name, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount)).ToList(),
            order.Total.Amount,
            fee.Amount,
            order.Total.Add(fee).Amount);
    }

    public static DeliveryDto ToDto(this Delivery delivery) =>
        new(delivery.Id, delivery.OrderId, delivery.AcceptedAtUtc, delivery.StartedAtUtc, delivery.DeliveredAtUtc);
}
