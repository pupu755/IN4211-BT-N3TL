using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;
using FoodHub.Domain.ValueObjects;

namespace FoodHub.Tests.Domain;

public class CouponTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ValidCoupon_Succeeds()
    {
        var coupon = Coupon.Create("DISCOUNT20", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));

        Assert.Equal("DISCOUNT20", coupon.Code);
        Assert.Equal(20000m, coupon.DiscountAmount.Amount);
        Assert.True(coupon.IsActive);
    }

    [Fact]
    public void CalculateDiscount_SubtotalMeetsMin_ReturnsDiscountAmount()
    {
        var coupon = Coupon.Create("DISCOUNT20", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));

        var discount = coupon.CalculateDiscount(Money.Of(150000m), Now);

        Assert.Equal(20000m, discount.Amount);
    }

    [Fact]
    public void CalculateDiscount_SubtotalBelowMin_ThrowsDomainException()
    {
        var coupon = Coupon.Create("DISCOUNT20", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));

        Assert.Throws<DomainException>(() => coupon.CalculateDiscount(Money.Of(80000m), Now));
    }

    [Fact]
    public void CalculateDiscount_Expired_ThrowsDomainException()
    {
        var coupon = Coupon.Create("DISCOUNT20", Money.Of(20000m), Money.Of(100000m), Now.AddDays(-1));

        Assert.Throws<DomainException>(() => coupon.CalculateDiscount(Money.Of(150000m), Now));
    }

    [Fact]
    public void CalculateDiscount_Inactive_ThrowsDomainException()
    {
        var coupon = Coupon.Create("DISCOUNT20", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));
        coupon.Deactivate();

        Assert.Throws<DomainException>(() => coupon.CalculateDiscount(Money.Of(150000m), Now));
    }

    [Fact]
    public void ApplyCoupon_ToPendingOrder_SetsDiscountAndCode()
    {
        var restaurant = Restaurant.Create(Guid.NewGuid(), "Pho 24", "1 Le Loi");
        var pho = restaurant.AddMenuItem("Pho", Money.Of(50000m));
        var order = Order.Create(Guid.NewGuid(), restaurant.Id, Now);
        order.AddItem(pho, 3); // 150,000

        var coupon = Coupon.Create("GIAM20K", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));
        order.ApplyCoupon(coupon, Now);

        Assert.Equal("GIAM20K", order.CouponCode);
        Assert.Equal(20000m, order.DiscountAmount.Amount);
        Assert.Equal(130000m, order.FinalAmount.Amount);
    }

    [Fact]
    public void RemoveCoupon_ResetsDiscount()
    {
        var restaurant = Restaurant.Create(Guid.NewGuid(), "Pho 24", "1 Le Loi");
        var pho = restaurant.AddMenuItem("Pho", Money.Of(50000m));
        var order = Order.Create(Guid.NewGuid(), restaurant.Id, Now);
        order.AddItem(pho, 3);

        var coupon = Coupon.Create("GIAM20K", Money.Of(20000m), Money.Of(100000m), Now.AddDays(7));
        order.ApplyCoupon(coupon, Now);
        order.RemoveCoupon();

        Assert.Null(order.CouponCode);
        Assert.Equal(0m, order.DiscountAmount.Amount);
        Assert.Equal(150000m, order.FinalAmount.Amount);
    }
}
