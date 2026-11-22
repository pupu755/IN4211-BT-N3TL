using FoodHub.Domain.Common;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Domain.Entities;

public class Delivery
{
    private Delivery()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ShipperProfileId { get; private set; }

    public DateTime AcceptedAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DeliveryCode? Code { get; private set; }

    /// <summary>Encrypted copy of the code so that only the customer can read it back. Never the plain code.</summary>
    public string? CustomerCodeCipher { get; private set; }

    public bool IsCompleted => DeliveredAtUtc.HasValue;

    internal static Delivery Create(Guid orderId, Guid shipperProfileId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        ShipperProfileId = shipperProfileId,
        AcceptedAtUtc = nowUtc
    };

    /// <summary>Creates the receipt code when delivery starts (QT03).</summary>
    public void StartDelivering(string plainCode, DateTime nowUtc, string? customerCodeCipher = null)
    {
        if (StartedAtUtc.HasValue)
        {
            throw new DomainException("Delivery has already started.");
        }

        Code = DeliveryCode.Create(plainCode, nowUtc);
        CustomerCodeCipher = customerCodeCipher;
        StartedAtUtc = nowUtc;
    }

    /// <summary>Checks the code given by the customer and completes the delivery on success (QT03).</summary>
    public CodeVerificationResult ConfirmReceipt(string inputCode, DateTime nowUtc)
    {
        if (Code is null)
        {
            throw new DomainException("Delivery has not started.");
        }

        if (IsCompleted)
        {
            return CodeVerificationResult.AlreadyUsed;
        }

        var result = Code.Verify(inputCode, nowUtc);
        if (result == CodeVerificationResult.Success)
        {
            DeliveredAtUtc = nowUtc;
        }

        return result;
    }
}
