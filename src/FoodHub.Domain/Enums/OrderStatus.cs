namespace FoodHub.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    ReadyForPickup = 3,
    Delivering = 4,
    Delivered = 5,
    Cancelled = 6
}
