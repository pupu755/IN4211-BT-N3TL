using FoodHub.Application.Dtos;
using FoodHub.Application.Strategies;
using FoodHub.Application.Validators;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Application;

public class DeliveryFeeStrategyTests
{
    [Fact]
    public void Calculate_FlatStrategy_ReturnsSameFeeForAnySubtotal()
    {
        var strategy = new FlatDeliveryFeeStrategy(Money.Of(12000m));

        Assert.Equal(12000m, strategy.Calculate(Money.Of(1m)).Amount);
        Assert.Equal(12000m, strategy.Calculate(Money.Of(900000m)).Amount);
    }

    [Fact]
    public void Calculate_BelowThreshold_ChargesFallbackFee()
    {
        var strategy = new FreeShippingOverThresholdStrategy(Money.Of(200000m), new FlatDeliveryFeeStrategy(Money.Of(15000m)));

        Assert.Equal(15000m, strategy.Calculate(Money.Of(199999m)).Amount);
    }

    [Fact]
    public void Calculate_AtThreshold_IsFree()
    {
        var strategy = new FreeShippingOverThresholdStrategy(Money.Of(200000m));

        Assert.Equal(0m, strategy.Calculate(Money.Of(200000m)).Amount);
    }

    [Fact]
    public void Calculate_DefaultStrategy_UsesDefaults()
    {
        var strategy = new FreeShippingOverThresholdStrategy();

        Assert.Equal(15000m, strategy.Calculate(Money.Of(50000m)).Amount);
        Assert.Equal(0m, strategy.Calculate(Money.Of(250000m)).Amount);
    }
}

public class ValidatorTests
{
    [Fact]
    public void Validate_RegisterWithShortPassword_Fails()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("a@b.com", "short", "An", Role.Customer, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RegisterShipperWithoutPlate_Fails()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("a@b.com", "password123", "An", Role.Shipper, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RegisterValidCustomer_Passes()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("a@b.com", "password123", "An", Role.Customer, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CreateOrderWithoutItems_Fails()
    {
        var result = new CreateOrderRequestValidator().Validate(new CreateOrderRequest(Guid.NewGuid(), Array.Empty<OrderLineRequest>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_CreateOrderWithZeroQuantity_Fails() // QT05
    {
        var request = new CreateOrderRequest(Guid.NewGuid(), new[] { new OrderLineRequest(Guid.NewGuid(), 0) });

        Assert.False(new CreateOrderRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Validate_AddMenuItemWithNegativePrice_Fails() // QT05
    {
        Assert.False(new AddMenuItemRequestValidator().Validate(new AddMenuItemRequest("Pho", -1m)).IsValid);
    }

    [Theory]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12345a", false)]
    [InlineData("123456", true)]
    public void Validate_ConfirmReceiptCode_ChecksSixDigits(string code, bool expected) // QT03
    {
        Assert.Equal(expected, new ConfirmReceiptRequestValidator().Validate(new ConfirmReceiptRequest(code)).IsValid);
    }
}

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsActive_BeforeExpiry_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7));

        Assert.True(token.IsActive(Now));
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7));

        Assert.False(token.IsActive(Now.AddDays(8)));
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7));

        token.Revoke(Now);

        Assert.False(token.IsActive(Now));
    }
}
