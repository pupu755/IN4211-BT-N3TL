using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Application.Services;

public sealed class RestaurantService
{
    private readonly IRestaurantRepository _restaurants;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateRestaurantRequest> _createValidator;
    private readonly IValidator<AddMenuItemRequest> _menuValidator;

    public RestaurantService(
        IRestaurantRepository restaurants,
        IUnitOfWork uow,
        IValidator<CreateRestaurantRequest> createValidator,
        IValidator<AddMenuItemRequest> menuValidator)
    {
        _restaurants = restaurants;
        _uow = uow;
        _createValidator = createValidator;
        _menuValidator = menuValidator;
    }

    public async Task<RestaurantDto> CreateAsync(Guid ownerId, CreateRestaurantRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_createValidator, request, ct);
        var restaurant = Restaurant.Create(ownerId, request.Name, request.Address);
        await _restaurants.AddAsync(restaurant, ct);
        await _uow.SaveChangesAsync(ct);
        return restaurant.ToDto();
    }

    public async Task<MenuItemDto> AddMenuItemAsync(Guid ownerId, Guid restaurantId, AddMenuItemRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_menuValidator, request, ct);
        var restaurant = await _restaurants.GetByIdAsync(restaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");
        if (restaurant.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this restaurant.");
        }

        var item = restaurant.AddMenuItem(request.Name, Money.Of(request.Price));
        await _uow.SaveChangesAsync(ct);
        return item.ToDto();
    }

    public async Task<IReadOnlyList<RestaurantDto>> ListAsync(CancellationToken ct = default) =>
        (await _restaurants.ListAsync(ct)).Select(r => r.ToDto()).ToList();

    public async Task<IReadOnlyList<RestaurantDto>> ListMineAsync(Guid ownerId, CancellationToken ct = default) =>
        (await _restaurants.ListByOwnerAsync(ownerId, ct)).Select(r => r.ToDto()).ToList();

    public async Task<RestaurantDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await _restaurants.GetByIdAsync(id, ct) ?? throw new NotFoundException("Restaurant not found.")).ToDto();
}
