using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Entities;

public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxCommentLength = 500;

    private Review()
    {
        Comment = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid RestaurantId { get; private set; }

    public int Rating { get; private set; }

    public string Comment { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Review Create(Order order, Guid customerId, int rating, string comment, DateTime nowUtc)
    {
        if (order.Status != OrderStatus.Delivered)
        {
            throw new DomainException("Only delivered orders can be reviewed.");
        }

        if (order.CustomerId != customerId)
        {
            throw new DomainException("This order belongs to another customer.");
        }

        if (rating is < MinRating or > MaxRating)
        {
            throw new DomainException($"Rating must be between {MinRating} and {MaxRating}.");
        }

        var trimmedComment = (comment ?? string.Empty).Trim();
        if (trimmedComment.Length > MaxCommentLength)
        {
            throw new DomainException($"Comment cannot exceed {MaxCommentLength} characters.");
        }

        return new Review
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = customerId,
            RestaurantId = order.RestaurantId,
            Rating = rating,
            Comment = trimmedComment,
            CreatedAtUtc = nowUtc
        };
    }
}
