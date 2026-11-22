namespace FoodHub.Domain.Interfaces;

/// <summary>Factory that produces a new random six digit receipt code.</summary>
public interface IDeliveryCodeFactory
{
    string Create();
}
