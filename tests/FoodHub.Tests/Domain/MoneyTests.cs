using FoodHub.Domain.Common;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Of_NegativeAmount_ThrowsDomainException() // QT05
    {
        Assert.Throws<DomainException>(() => Money.Of(-0.01m));
    }

    [Fact]
    public void Of_Zero_ReturnsZeroAmount() // QT05
    {
        Assert.Equal(0m, Money.Of(0m).Amount);
    }

    [Fact]
    public void Of_ValueWithManyDecimals_RoundsToTwoDecimals() // QT05
    {
        Assert.Equal(10.13m, Money.Of(10.126m).Amount);
    }

    [Fact]
    public void Add_TwoAmounts_ReturnsSum() // QT05
    {
        Assert.Equal(7.5m, Money.Of(2.5m).Add(Money.Of(5m)).Amount);
    }

    [Fact]
    public void Multiply_ByQuantity_ReturnsProduct() // QT05
    {
        Assert.Equal(90000m, Money.Of(30000m).Multiply(3).Amount);
    }

    [Fact]
    public void Multiply_ByNegativeQuantity_ThrowsDomainException() // QT05
    {
        Assert.Throws<DomainException>(() => Money.Of(10m).Multiply(-1));
    }

    [Fact]
    public void Equals_SameAmount_ReturnsTrue()
    {
        Assert.Equal(Money.Of(5m), Money.Of(5m));
    }
}
