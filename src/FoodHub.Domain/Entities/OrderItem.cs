using FoodHub.Domain.Common;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class OrderItem
{
    private OrderItem()
    {
        Name = string.Empty;
        UnitPrice = Money.Zero;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid MenuItemId { get; private set; }

    public string Name { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>Unit price multiplied by quantity (QT05).</summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    internal static OrderItem Create(Guid orderId, MenuItem menuItem, int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            MenuItemId = menuItem.Id,
            Name = menuItem.Name,
            UnitPrice = menuItem.Price,
            Quantity = quantity
        };
    }

    internal void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Quantity += quantity;
    }
}
