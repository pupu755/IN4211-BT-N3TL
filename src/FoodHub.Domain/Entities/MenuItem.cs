using FoodHub.Domain.Common;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class MenuItem
{
    private MenuItem()
    {
        Name = string.Empty;
        Price = Money.Zero;
    }

    public Guid Id { get; private set; }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; }

    public Money Price { get; private set; }

    public bool IsAvailable { get; private set; }

    public static MenuItem Create(Guid restaurantId, string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Menu item name is required.");
        }

        return new MenuItem
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurantId,
            Name = name.Trim(),
            Price = price,
            IsAvailable = true
        };
    }

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;

    public void ChangePrice(Money price) => Price = price;
}
