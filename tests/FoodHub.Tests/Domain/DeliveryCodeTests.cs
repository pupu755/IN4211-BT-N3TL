using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Domain;

public class DeliveryCodeTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12a456")]
    [InlineData("")]
    public void Create_InvalidFormat_ThrowsDomainException(string code) // QT03
    {
        Assert.Throws<DomainException>(() => DeliveryCode.Create(code, Now));
    }

    [Fact]
    public void Create_ValidCode_StoresHashNotPlainText() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        Assert.DoesNotContain("123456", code.Hash);
        Assert.NotEmpty(code.Salt);
    }

    [Fact]
    public void Create_SamePlainCodeTwice_ProducesDifferentHashes() // QT03
    {
        var first = DeliveryCode.Create("123456", Now);
        var second = DeliveryCode.Create("123456", Now);

        Assert.NotEqual(first.Hash, second.Hash);
    }

    [Fact]
    public void Create_ValidCode_ExpiresAfterTwoHours() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        Assert.Equal(Now.AddHours(2), code.ExpiresAtUtc);
    }

    [Fact]
    public void Verify_CorrectCode_ReturnsSuccess() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        Assert.Equal(CodeVerificationResult.Success, code.Verify("123456", Now.AddMinutes(10)));
    }

    [Fact]
    public void Verify_CorrectCodeTwice_SecondCallReturnsAlreadyUsed() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);
        code.Verify("123456", Now);

        Assert.Equal(CodeVerificationResult.AlreadyUsed, code.Verify("123456", Now));
    }

    [Fact]
    public void Verify_WrongCode_ReturnsWrongAndCountsAttempt() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        var result = code.Verify("000000", Now);

        Assert.Equal(CodeVerificationResult.Wrong, result);
        Assert.Equal(1, code.FailedAttempts);
    }

    [Fact]
    public void Verify_AfterExpiry_ReturnsExpired() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        Assert.Equal(CodeVerificationResult.Expired, code.Verify("123456", Now.AddHours(2).AddSeconds(1)));
    }

    [Fact]
    public void Verify_ExactlyAtExpiry_StillAccepted() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        Assert.Equal(CodeVerificationResult.Success, code.Verify("123456", Now.AddHours(2)));
    }

    [Fact]
    public void Verify_FiveWrongAttempts_LocksCode() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);

        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            code.Verify("000000", Now);
        }

        Assert.True(code.IsLocked);
    }

    [Fact]
    public void Verify_CorrectCodeAfterLock_ReturnsLocked() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);
        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            code.Verify("000000", Now);
        }

        Assert.Equal(CodeVerificationResult.Locked, code.Verify("123456", Now));
    }

    [Fact]
    public void Verify_FourWrongThenCorrect_ReturnsSuccess() // QT03
    {
        var code = DeliveryCode.Create("123456", Now);
        for (var i = 0; i < DeliveryCode.MaxFailedAttempts - 1; i++)
        {
            code.Verify("000000", Now);
        }

        Assert.Equal(CodeVerificationResult.Success, code.Verify("123456", Now));
    }
}

public class DeliveryTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Delivery CreateDelivery() =>
        ShipperProfile.Create(Guid.NewGuid(), "59A1-12345").AcceptOrder(Guid.NewGuid(), Now);

    [Fact]
    public void StartDelivering_FirstTime_CreatesCode() // QT03
    {
        var delivery = CreateDelivery();

        delivery.StartDelivering("123456", Now);

        Assert.NotNull(delivery.Code);
        Assert.Equal(Now, delivery.StartedAtUtc);
    }

    [Fact]
    public void StartDelivering_Twice_ThrowsDomainException() // QT03
    {
        var delivery = CreateDelivery();
        delivery.StartDelivering("123456", Now);

        Assert.Throws<DomainException>(() => delivery.StartDelivering("654321", Now));
    }

    [Fact]
    public void ConfirmReceipt_BeforeStart_ThrowsDomainException() // QT03
    {
        Assert.Throws<DomainException>(() => CreateDelivery().ConfirmReceipt("123456", Now));
    }

    [Fact]
    public void ConfirmReceipt_CorrectCode_CompletesDelivery() // QT03
    {
        var delivery = CreateDelivery();
        delivery.StartDelivering("123456", Now);

        var result = delivery.ConfirmReceipt("123456", Now.AddMinutes(30));

        Assert.Equal(CodeVerificationResult.Success, result);
        Assert.True(delivery.IsCompleted);
    }

    [Fact]
    public void ConfirmReceipt_WrongCode_DoesNotComplete() // QT03
    {
        var delivery = CreateDelivery();
        delivery.StartDelivering("123456", Now);

        var result = delivery.ConfirmReceipt("000000", Now);

        Assert.Equal(CodeVerificationResult.Wrong, result);
        Assert.False(delivery.IsCompleted);
    }

    [Fact]
    public void ConfirmReceipt_AfterCompleted_ReturnsAlreadyUsed() // QT03
    {
        var delivery = CreateDelivery();
        delivery.StartDelivering("123456", Now);
        delivery.ConfirmReceipt("123456", Now);

        Assert.Equal(CodeVerificationResult.AlreadyUsed, delivery.ConfirmReceipt("123456", Now));
    }
}
