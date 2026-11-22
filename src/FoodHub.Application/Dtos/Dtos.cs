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
    string? CouponCode,
    decimal DiscountAmount,
    decimal DeliveryFee,
    decimal GrandTotal);

public sealed record ConfirmReceiptRequest(string Code);

public sealed record DeliveryDto(Guid Id, Guid OrderId, DateTime AcceptedAtUtc, DateTime? StartedAtUtc, DateTime? DeliveredAtUtc);

public sealed record DeliveryCodeDto(string Code, DateTime ExpiresAtUtc, bool IsUsed, bool IsLocked);

public sealed record CreateReviewRequest(int Rating, string Comment);

public sealed record ReviewDto(Guid Id, Guid OrderId, Guid CustomerId, Guid RestaurantId, int Rating, string Comment, DateTime CreatedAtUtc);

public sealed record CreateCouponRequest(string Code, decimal DiscountAmount, decimal MinOrderAmount, DateTime ExpiresAtUtc);

public sealed record ApplyCouponRequest(string Code);

public sealed record CouponDto(Guid Id, string Code, decimal DiscountAmount, decimal MinOrderAmount, DateTime ExpiresAtUtc, bool IsActive);

public sealed record RevenueTopItemDto(Guid MenuItemId, string Name, int QuantitySold, decimal TotalAmount);

public sealed record RestaurantRevenueDto(
    Guid RestaurantId,
    int TotalDeliveredOrders,
    decimal TotalRevenue,
    int CancelledOrders,
    IReadOnlyList<RevenueTopItemDto> TopSellingItems);
