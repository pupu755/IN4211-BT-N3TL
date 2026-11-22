using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Services;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IRestaurantRepository _restaurants;
    private readonly IDeliveryRepository _deliveries;
    private readonly IShipperRepository _shippers;
    private readonly IDeliveryFeeStrategy _feeStrategy;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ICouponRepository _coupons;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<ApplyCouponRequest> _applyCouponValidator;

    public OrderService(
        IOrderRepository orders,
        IRestaurantRepository restaurants,
        IDeliveryRepository deliveries,
        IShipperRepository shippers,
        IDeliveryFeeStrategy feeStrategy,
        IUnitOfWork uow,
        IClock clock,
        ICouponRepository coupons,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<ApplyCouponRequest> applyCouponValidator)
    {
        _orders = orders;
        _restaurants = restaurants;
        _deliveries = deliveries;
        _shippers = shippers;
        _feeStrategy = feeStrategy;
        _uow = uow;
        _clock = clock;
        _coupons = coupons;
        _createValidator = createValidator;
        _applyCouponValidator = applyCouponValidator;
    }

    public async Task<OrderDto> CreateAsync(Guid customerId, CreateOrderRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_createValidator, request, ct);

        var restaurant = await _restaurants.GetByIdAsync(request.RestaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");

        var ids = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = (await _restaurants.GetMenuItemsAsync(ids, ct)).ToDictionary(m => m.Id);
        if (ids.Any(id => !menuItems.ContainsKey(id)))
        {
            throw new NotFoundException("One or more menu items were not found.");
        }

        var order = Order.Create(customerId, restaurant.Id, _clock.UtcNow);
        foreach (var line in request.Items)
        {
            order.AddItem(menuItems[line.MenuItemId], line.Quantity); // QT01 is enforced inside Order
        }

        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);
        return order.ToDto(_feeStrategy);
    }

    public async Task<OrderDto> GetAsync(Guid userId, Role role, Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        await EnsureCanViewAsync(order, userId, role, ct);
        return order.ToDto(_feeStrategy);
    }

    public async Task<IReadOnlyList<OrderDto>> ListMineAsync(Guid customerId, CancellationToken ct = default) =>
        (await _orders.ListByCustomerAsync(customerId, ct)).Select(o => o.ToDto(_feeStrategy)).ToList();

    public async Task<IReadOnlyList<OrderDto>> ListForRestaurantAsync(Guid ownerId, Guid restaurantId, CancellationToken ct = default)
    {
        await EnsureOwnerAsync(restaurantId, ownerId, ct);
        return (await _orders.ListByRestaurantAsync(restaurantId, ct)).Select(o => o.ToDto(_feeStrategy)).ToList();
    }

    public Task<OrderDto> ConfirmAsync(Guid ownerId, Guid orderId, CancellationToken ct = default) =>
        AdvanceAsync(ownerId, orderId, o => o.Confirm(), ct);

    public Task<OrderDto> StartPreparingAsync(Guid ownerId, Guid orderId, CancellationToken ct = default) =>
        AdvanceAsync(ownerId, orderId, o => o.StartPreparing(), ct);

    public Task<OrderDto> MarkReadyAsync(Guid ownerId, Guid orderId, CancellationToken ct = default) =>
        AdvanceAsync(ownerId, orderId, o => o.MarkReadyForPickup(), ct);

    public async Task<OrderDto> ApplyCouponAsync(Guid customerId, Guid orderId, ApplyCouponRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_applyCouponValidator, request, ct);
        var order = await LoadAsync(orderId, ct);
        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException("This order belongs to another customer.");
        }

        var coupon = await _coupons.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException("Coupon not found.");

        order.ApplyCoupon(coupon, _clock.UtcNow);
        await _uow.SaveChangesAsync(ct);
        return order.ToDto(_feeStrategy);
    }

    public async Task<OrderDto> RemoveCouponAsync(Guid customerId, Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.CustomerId != customerId)
        {
            throw new ForbiddenException("This order belongs to another customer.");
        }

        order.RemoveCoupon();
        await _uow.SaveChangesAsync(ct);
        return order.ToDto(_feeStrategy);
    }

    /// <summary>The customer or the restaurant owner may cancel, but only before Preparing (QT04).</summary>
    public async Task<OrderDto> CancelAsync(Guid userId, Role role, Guid orderId, CancellationToken ct = default)
    {
        var order = await LoadAsync(orderId, ct);
        if (role == Role.Customer)
        {
            if (order.CustomerId != userId)
            {
                throw new ForbiddenException("This order belongs to another customer.");
            }
        }
        else if (role == Role.Restaurant)
        {
            await EnsureOwnerAsync(order.RestaurantId, userId, ct);
        }
        else if (role != Role.Admin)
        {
            throw new ForbiddenException("This role cannot cancel orders.");
        }

        order.Cancel();
        await _uow.SaveChangesAsync(ct);
        return order.ToDto(_feeStrategy);
    }

    private async Task<OrderDto> AdvanceAsync(Guid ownerId, Guid orderId, Action<Order> action, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        await EnsureOwnerAsync(order.RestaurantId, ownerId, ct);
        action(order);
        await _uow.SaveChangesAsync(ct);
        return order.ToDto(_feeStrategy);
    }

    private async Task<Order> LoadAsync(Guid orderId, CancellationToken ct) =>
        await _orders.GetByIdAsync(orderId, ct) ?? throw new NotFoundException("Order not found.");

    private async Task EnsureOwnerAsync(Guid restaurantId, Guid ownerId, CancellationToken ct)
    {
        var restaurant = await _restaurants.GetByIdAsync(restaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");
        if (restaurant.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this restaurant.");
        }
    }

    private async Task EnsureCanViewAsync(Order order, Guid userId, Role role, CancellationToken ct)
    {
        switch (role)
        {
            case Role.Admin:
                return;
            case Role.Customer when order.CustomerId == userId:
                return;
            case Role.Restaurant:
                await EnsureOwnerAsync(order.RestaurantId, userId, ct);
                return;
            case Role.Shipper:
                var profile = await _shippers.GetByUserIdAsync(userId, ct);
                var delivery = await _deliveries.GetByOrderIdAsync(order.Id, ct);
                if (order.Status == OrderStatus.ReadyForPickup && delivery is null)
                {
                    return;
                }

                if (profile is not null && delivery?.ShipperProfileId == profile.Id)
                {
                    return;
                }

                break;
        }

        throw new ForbiddenException("You cannot view this order.");
    }
}
