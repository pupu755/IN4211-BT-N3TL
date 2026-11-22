using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public interface ICodeProtector
{
    string Protect(string plainCode);

    string Unprotect(string cipher);
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user);

    /// <summary>Creates a random refresh token and returns the plain value together with its hash.</summary>
    (string Plain, string Hash) CreateRefreshToken();

    string HashRefreshToken(string plainToken);

    TimeSpan RefreshTokenLifetime { get; }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);
}

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Restaurant>> ListAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Restaurant>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task AddAsync(Restaurant restaurant, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> ListByRestaurantAsync(Guid restaurantId, CancellationToken ct = default);

    /// <summary>Orders in ReadyForPickup that no shipper has accepted yet.</summary>
    Task<IReadOnlyList<Order>> ListAvailableForDeliveryAsync(CancellationToken ct = default);

    Task AddAsync(Order order, CancellationToken ct = default);
}

public interface IShipperRepository
{
    /// <summary>Loads the profile together with its deliveries.</summary>
    Task<ShipperProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(ShipperProfile profile, CancellationToken ct = default);
}

public interface IDeliveryRepository
{
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}

public interface IReviewRepository
{
    Task<Review?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<IReadOnlyList<Review>> ListByRestaurantAsync(Guid restaurantId, CancellationToken ct = default);

    Task AddAsync(Review review, CancellationToken ct = default);
}

public interface ICouponRepository
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<Coupon>> ListActiveAsync(CancellationToken ct = default);

    Task AddAsync(Coupon coupon, CancellationToken ct = default);
}

/// <summary>Strategy that decides the delivery fee for an order subtotal.</summary>
public interface IDeliveryFeeStrategy
{
    FoodHub.Domain.ValueObjects.Money Calculate(FoodHub.Domain.ValueObjects.Money subtotal);
}
