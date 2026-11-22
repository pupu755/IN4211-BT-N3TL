using FoodHub.Domain.Common;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class Coupon
{
    private Coupon()
    {
        Code = string.Empty;
        DiscountAmount = Money.Zero;
        MinOrderAmount = Money.Zero;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; }

    public Money DiscountAmount { get; private set; }

    public Money MinOrderAmount { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public static Coupon Create(string code, Money discountAmount, Money minOrderAmount, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Coupon code is required.");
        }

        if (discountAmount.Amount <= 0)
        {
            throw new DomainException("Discount amount must be greater than zero.");
        }

        return new Coupon
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            DiscountAmount = discountAmount,
            MinOrderAmount = minOrderAmount,
            ExpiresAtUtc = expiresAtUtc,
            IsActive = true
        };
    }

    public Money CalculateDiscount(Money orderSubtotal, DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new DomainException("Coupon is no longer active.");
        }

        if (nowUtc > ExpiresAtUtc)
        {
            throw new DomainException("Coupon has expired.");
        }

        if (orderSubtotal.Amount < MinOrderAmount.Amount)
        {
            throw new DomainException($"Order total must be at least {MinOrderAmount.Amount} to apply coupon.");
        }

        return orderSubtotal.Amount < DiscountAmount.Amount ? orderSubtotal : DiscountAmount;
    }

    public void Deactivate() => IsActive = false;
}
