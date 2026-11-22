using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Repositories;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db) => _db = db;

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

internal sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) => await _db.Users.AddAsync(user, ct);
}

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);
}

internal sealed class RestaurantRepository : IRestaurantRepository
{
    private readonly AppDbContext _db;

    public RestaurantRepository(AppDbContext db) => _db = db;

    public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Restaurants.Include(r => r.MenuItems).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Restaurant>> ListAsync(CancellationToken ct = default) =>
        await _db.Restaurants.Include(r => r.MenuItems).OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Restaurant>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await _db.Restaurants.Include(r => r.MenuItems).Where(r => r.OwnerId == ownerId).OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<MenuItem>> GetMenuItemsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = ids.ToList();
        return await _db.MenuItems.Where(m => list.Contains(m.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Restaurant restaurant, CancellationToken ct = default) =>
        await _db.Restaurants.AddAsync(restaurant, ct);
}

internal sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    private IQueryable<Order> Orders => _db.Orders.Include(o => o.Items);

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await Orders.Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> ListByRestaurantAsync(Guid restaurantId, CancellationToken ct = default) =>
        await Orders.Where(o => o.RestaurantId == restaurantId).OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> ListAvailableForDeliveryAsync(CancellationToken ct = default) =>
        await Orders
            .Where(o => o.Status == OrderStatus.ReadyForPickup && !_db.Deliveries.Any(d => d.OrderId == o.Id))
            .OrderBy(o => o.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) => await _db.Orders.AddAsync(order, ct);
}

internal sealed class ShipperRepository : IShipperRepository
{
    private readonly AppDbContext _db;

    public ShipperRepository(AppDbContext db) => _db = db;

    public Task<ShipperProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.ShipperProfiles.Include(s => s.Deliveries).FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task AddAsync(ShipperProfile profile, CancellationToken ct = default) =>
        await _db.ShipperProfiles.AddAsync(profile, ct);
}

internal sealed class DeliveryRepository : IDeliveryRepository
{
    private readonly AppDbContext _db;

    public DeliveryRepository(AppDbContext db) => _db = db;

    public Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default) =>
        _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId, ct);
}
