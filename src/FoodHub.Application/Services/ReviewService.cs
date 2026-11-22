using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Services;

public sealed class ReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IValidator<CreateReviewRequest> _validator;

    public ReviewService(
        IReviewRepository reviews,
        IOrderRepository orders,
        IUnitOfWork uow,
        IClock clock,
        IValidator<CreateReviewRequest> validator)
    {
        _reviews = reviews;
        _orders = orders;
        _uow = uow;
        _clock = clock;
        _validator = validator;
    }

    public async Task<ReviewDto> CreateAsync(Guid customerId, Guid orderId, CreateReviewRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_validator, request, ct);

        var order = await _orders.GetByIdAsync(orderId, ct)
            ?? throw new NotFoundException("Order not found.");

        if (await _reviews.GetByOrderIdAsync(orderId, ct) is not null)
        {
            throw new ConflictException("This order has already been reviewed.");
        }

        var review = Review.Create(order, customerId, request.Rating, request.Comment, _clock.UtcNow);
        await _reviews.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);
        return review.ToDto();
    }

    public async Task<IReadOnlyList<ReviewDto>> ListByRestaurantAsync(Guid restaurantId, CancellationToken ct = default) =>
        (await _reviews.ListByRestaurantAsync(restaurantId, ct)).Select(r => r.ToDto()).ToList();
}
