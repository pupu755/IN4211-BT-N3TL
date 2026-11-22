using FoodHub.Domain.Common;

namespace FoodHub.Domain.ValueObjects;

/// <summary>Non-negative amount of money, backed by decimal (QT05).</summary>
public sealed record Money
{
    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money Zero { get; } = new(0m);

    public static Money Of(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException("Money cannot be negative.");
        }

        return new Money(decimal.Round(amount, 2));
    }

    public Money Add(Money other) => Of(Amount + other.Amount);

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainException("Quantity cannot be negative.");
        }

        return Of(Amount * quantity);
    }

    public override string ToString() => Amount.ToString("0.00");
}
