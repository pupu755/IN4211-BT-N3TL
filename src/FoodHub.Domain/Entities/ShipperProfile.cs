using FoodHub.Domain.Common;

namespace FoodHub.Domain.Entities;

public class ShipperProfile
{
    public const int MaxActiveOrders = 2;

    private readonly List<Delivery> _deliveries = new();

    private ShipperProfile()
    {
        VehiclePlate = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string VehiclePlate { get; private set; }

    public IReadOnlyCollection<Delivery> Deliveries => _deliveries;

    public int ActiveDeliveryCount => _deliveries.Count(d => !d.IsCompleted);

    public static ShipperProfile Create(Guid userId, string vehiclePlate)
    {
        if (string.IsNullOrWhiteSpace(vehiclePlate))
        {
            throw new DomainException("Vehicle plate is required.");
        }

        return new ShipperProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VehiclePlate = vehiclePlate.Trim()
        };
    }

    /// <summary>A shipper may hold at most two undelivered orders at once (QT02).</summary>
    public Delivery AcceptOrder(Guid orderId, DateTime nowUtc)
    {
        if (_deliveries.Any(d => d.OrderId == orderId))
        {
            throw new DomainException("This order was already accepted by the shipper.");
        }

        if (ActiveDeliveryCount >= MaxActiveOrders)
        {
            throw new DomainException($"A shipper can hold at most {MaxActiveOrders} undelivered orders.");
        }

        var delivery = Delivery.Create(orderId, Id, nowUtc);
        _deliveries.Add(delivery);
        return delivery;
    }
}
