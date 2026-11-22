using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = new();

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid RestaurantId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    /// <summary>Sum of unit price times quantity over all items (QT05).</summary>
    public Money Total => _items.Aggregate(Money.Zero, (sum, item) => sum.Add(item.LineTotal));

    public static Order Create(Guid customerId, Guid restaurantId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        RestaurantId = restaurantId,
        Status = OrderStatus.Pending,
        CreatedAtUtc = nowUtc
    };

    /// <summary>Adds a dish to the order. All dishes must come from the same restaurant (QT01).</summary>
    public void AddItem(MenuItem menuItem, int quantity)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException("Items can only be added while the order is pending.");
        }

        if (menuItem.RestaurantId != RestaurantId)
        {
            throw new DomainException("An order can only contain items from one restaurant.");
        }

        if (!menuItem.IsAvailable)
        {
            throw new DomainException("Menu item is not available.");
        }

        var existing = _items.FirstOrDefault(i => i.MenuItemId == menuItem.Id);
        if (existing is null)
        {
            _items.Add(OrderItem.Create(Id, menuItem, quantity));
        }
        else
        {
            existing.IncreaseQuantity(quantity);
        }
    }

    public void Confirm()
    {
        if (_items.Count == 0)
        {
            throw new DomainException("An empty order cannot be confirmed.");
        }

        Transition(OrderStatus.Pending, OrderStatus.Confirmed);
    }

    public void StartPreparing() => Transition(OrderStatus.Confirmed, OrderStatus.Preparing);

    public void MarkReadyForPickup() => Transition(OrderStatus.Preparing, OrderStatus.ReadyForPickup);

    public void StartDelivering() => Transition(OrderStatus.ReadyForPickup, OrderStatus.Delivering);

    public void MarkDelivered() => Transition(OrderStatus.Delivering, OrderStatus.Delivered);

    /// <summary>An order can only be cancelled before it reaches Preparing (QT04).</summary>
    public void Cancel()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
        {
            throw new DomainException($"An order in status {Status} cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    /// <summary>Statuses only move forward in a fixed order (QT04).</summary>
    private void Transition(OrderStatus expectedCurrent, OrderStatus next)
    {
        if (Status != expectedCurrent)
        {
            throw new DomainException($"Cannot move order from {Status} to {next}.");
        }

        Status = next;
    }
}
