using FoodHub.Domain.Common;
using FoodHub.Domain.Entities;

namespace FoodHub.Tests.Domain;

public class ShipperProfileTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    private static ShipperProfile CreateShipper() => ShipperProfile.Create(Guid.NewGuid(), "59A1-12345");

    [Fact]
    public void AcceptOrder_FirstOrder_CreatesActiveDelivery() // QT02
    {
        var shipper = CreateShipper();

        shipper.AcceptOrder(Guid.NewGuid(), Now);

        Assert.Equal(1, shipper.ActiveDeliveryCount);
    }

    [Fact]
    public void AcceptOrder_SecondOrder_IsAllowed() // QT02
    {
        var shipper = CreateShipper();
        shipper.AcceptOrder(Guid.NewGuid(), Now);

        shipper.AcceptOrder(Guid.NewGuid(), Now);

        Assert.Equal(2, shipper.ActiveDeliveryCount);
    }

    [Fact]
    public void AcceptOrder_ThirdUndeliveredOrder_ThrowsDomainException() // QT02
    {
        var shipper = CreateShipper();
        shipper.AcceptOrder(Guid.NewGuid(), Now);
        shipper.AcceptOrder(Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() => shipper.AcceptOrder(Guid.NewGuid(), Now));
    }

    [Fact]
    public void AcceptOrder_AfterOneDelivered_IsAllowed() // QT02
    {
        var shipper = CreateShipper();
        var first = shipper.AcceptOrder(Guid.NewGuid(), Now);
        shipper.AcceptOrder(Guid.NewGuid(), Now);
        first.StartDelivering("123456", Now);
        first.ConfirmReceipt("123456", Now);

        shipper.AcceptOrder(Guid.NewGuid(), Now);

        Assert.Equal(2, shipper.ActiveDeliveryCount);
    }

    [Fact]
    public void AcceptOrder_SameOrderTwice_ThrowsDomainException()
    {
        var shipper = CreateShipper();
        var orderId = Guid.NewGuid();
        shipper.AcceptOrder(orderId, Now);

        Assert.Throws<DomainException>(() => shipper.AcceptOrder(orderId, Now));
    }

    [Fact]
    public void Create_EmptyVehiclePlate_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ShipperProfile.Create(Guid.NewGuid(), " "));
    }
}
