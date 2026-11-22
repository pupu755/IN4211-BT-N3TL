using FoodHub.Application.Interfaces;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Application.Strategies;

/// <summary>Charges the same fee for every order.</summary>
public sealed class FlatDeliveryFeeStrategy : IDeliveryFeeStrategy
{
    public static readonly Money DefaultFee = Money.Of(15000m);

    private readonly Money _fee;

    public FlatDeliveryFeeStrategy(Money? fee = null)
    {
        _fee = fee ?? DefaultFee;
    }

    public Money Calculate(Money subtotal) => _fee;
}

/// <summary>Free delivery when the subtotal reaches a threshold, otherwise a flat fee.</summary>
public sealed class FreeShippingOverThresholdStrategy : IDeliveryFeeStrategy
{
    public static readonly Money DefaultThreshold = Money.Of(200000m);

    private readonly Money _threshold;
    private readonly IDeliveryFeeStrategy _fallback;

    public FreeShippingOverThresholdStrategy(Money? threshold = null, IDeliveryFeeStrategy? fallback = null)
    {
        _threshold = threshold ?? DefaultThreshold;
        _fallback = fallback ?? new FlatDeliveryFeeStrategy();
    }

    public Money Calculate(Money subtotal) =>
        subtotal.Amount >= _threshold.Amount ? Money.Zero : _fallback.Calculate(subtotal);
}
