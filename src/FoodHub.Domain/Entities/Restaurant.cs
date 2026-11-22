using FoodHub.Domain.Common;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class Restaurant
{
    private readonly List<MenuItem> _menuItems = new();

    private Restaurant()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public IReadOnlyCollection<MenuItem> MenuItems => _menuItems;

    public static Restaurant Create(Guid ownerId, string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Restaurant name is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Restaurant address is required.");
        }

        return new Restaurant
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Address = address.Trim()
        };
    }

    public MenuItem AddMenuItem(string name, Money price)
    {
        var item = MenuItem.Create(Id, name, price);
        _menuItems.Add(item);
        return item;
    }
}
