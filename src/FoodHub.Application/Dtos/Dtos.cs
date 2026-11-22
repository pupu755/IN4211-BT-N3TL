using FoodHub.Domain.Enums;

namespace FoodHub.Application.Dtos;

public sealed record RegisterRequest(string Email, string Password, string FullName, Role Role, string? VehiclePlate);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(Guid UserId, Role Role, string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken);

public sealed record CreateRestaurantRequest(string Name, string Address);

public sealed record AddMenuItemRequest(string Name, decimal Price);

public sealed record MenuItemDto(Guid Id, string Name, decimal Price, bool IsAvailable);

public sealed record RestaurantDto(Guid Id, string Name, string Address, IReadOnlyList<MenuItemDto> MenuItems);

public sealed record OrderLineRequest(Guid MenuItemId, int Quantity);

public sealed record CreateOrderRequest(Guid RestaurantId, IReadOnlyList<OrderLineRequest> Items);

public sealed record OrderItemDto(Guid MenuItemId, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    OrderStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal GrandTotal);

public sealed record ConfirmReceiptRequest(string Code);

public sealed record DeliveryDto(Guid Id, Guid OrderId, DateTime AcceptedAtUtc, DateTime? StartedAtUtc, DateTime? DeliveredAtUtc);

public sealed record DeliveryCodeDto(string Code, DateTime ExpiresAtUtc, bool IsUsed, bool IsLocked);
