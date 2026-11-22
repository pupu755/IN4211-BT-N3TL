using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.Interfaces;
using FoodHub.Domain.ValueObjects;
using FoodHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Tests.Infrastructure;

public class SecurityServiceTests
{
    [Fact]
    public void Hash_ThenVerify_WithSamePassword_ReturnsTrue()
    {
        using var host = new TestHost();
        var hasher = host.Services.GetRequiredService<IPasswordHasher>();

        Assert.True(hasher.Verify("Secret#123", hasher.Hash("Secret#123")));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        using var host = new TestHost();
        var hasher = host.Services.GetRequiredService<IPasswordHasher>();

        Assert.False(hasher.Verify("other", hasher.Hash("Secret#123")));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        using var host = new TestHost();
        var hasher = host.Services.GetRequiredService<IPasswordHasher>();

        Assert.NotEqual(hasher.Hash("Secret#123"), hasher.Hash("Secret#123"));
    }

    [Fact]
    public void CreateAccessToken_ContainsUserIdAndRoleClaims()
    {
        using var host = new TestHost();
        var jwt = host.Services.GetRequiredService<IJwtTokenService>();
        var user = User.Create("a@b.com", "hash", "An", Role.Restaurant);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt.CreateAccessToken(user).Value);

        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("Restaurant", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void CreateRefreshToken_ReturnsPlainAndMatchingHash()
    {
        using var host = new TestHost();
        var jwt = host.Services.GetRequiredService<IJwtTokenService>();

        var (plain, hash) = jwt.CreateRefreshToken();

        Assert.NotEqual(plain, hash);
        Assert.Equal(hash, jwt.HashRefreshToken(plain));
    }

    [Fact]
    public void Protect_ThenUnprotect_ReturnsOriginalCode() // QT03
    {
        using var host = new TestHost();
        var protector = host.Services.GetRequiredService<ICodeProtector>();

        Assert.Equal("042917", protector.Unprotect(protector.Protect("042917")));
    }

    [Fact]
    public void Unprotect_TamperedCipher_Throws() // QT03
    {
        using var host = new TestHost();
        var protector = host.Services.GetRequiredService<ICodeProtector>();
        var bytes = Convert.FromBase64String(protector.Protect("042917"));
        bytes[^1] ^= 0xFF;

        Assert.ThrowsAny<Exception>(() => protector.Unprotect(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Create_ManyCodes_AreAlwaysSixDigits() // QT03
    {
        using var host = new TestHost();
        var factory = host.Services.GetRequiredService<IDeliveryCodeFactory>();

        for (var i = 0; i < 200; i++)
        {
            var code = factory.Create();
            Assert.Equal(6, code.Length);
            Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
        }
    }
}

public class PersistenceTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SaveOrder_WithItems_ReloadsWithSameTotalAndStatus() // QT05
    {
        using var host = new TestHost();
        Guid orderId;
        using (var scope = host.NewScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var owner = User.Create("owner@x.com", "h", "Owner", Role.Restaurant);
            var customer = User.Create("cus@x.com", "h", "Cus", Role.Customer);
            var restaurant = Restaurant.Create(owner.Id, "Pho 24", "1 Le Loi");
            var pho = restaurant.AddMenuItem("Pho", Money.Of(50000m));
            var tea = restaurant.AddMenuItem("Tea", Money.Of(10000m));
            var order = Order.Create(customer.Id, restaurant.Id, Now);
            order.AddItem(pho, 2);
            order.AddItem(tea, 3);
            order.Confirm();
            db.AddRange(owner, customer, restaurant, order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        using var verify = host.NewScope();
        var loaded = await verify.ServiceProvider.GetRequiredService<IOrderRepository>().GetByIdAsync(orderId);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Items.Count);
        Assert.Equal(130000m, loaded.Total.Amount);
        Assert.Equal(OrderStatus.Confirmed, loaded.Status);
    }

    [Fact]
    public async Task SaveShipperDelivery_AddedThroughProfile_IsPersistedWithCodeHash() // QT02 QT03
    {
        using var host = new TestHost();
        Guid userId;
        Guid orderId = Guid.NewGuid();
        using (var scope = host.NewScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = User.Create("ship@x.com", "h", "Ship", Role.Shipper);
            var profile = ShipperProfile.Create(user.Id, "59A1-12345");
            var owner = User.Create("owner@x.com", "h", "Owner", Role.Restaurant);
            var restaurant = Restaurant.Create(owner.Id, "Pho 24", "1 Le Loi");
            var order = Order.Create(user.Id, restaurant.Id, Now);
            orderId = order.Id;
            db.AddRange(user, owner, restaurant, order, profile);
            await db.SaveChangesAsync();
            userId = user.Id;
        }

        using (var scope = host.NewScope())
        {
            var shippers = scope.ServiceProvider.GetRequiredService<IShipperRepository>();
            var profile = (await shippers.GetByUserIdAsync(userId))!;
            var delivery = profile.AcceptOrder(orderId, Now);
            delivery.StartDelivering("123456", Now, "cipher");
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using var verify = host.NewScope();
        var reloaded = (await verify.ServiceProvider.GetRequiredService<IShipperRepository>().GetByUserIdAsync(userId))!;
        var saved = Assert.Single(reloaded.Deliveries);
        Assert.Equal(1, reloaded.ActiveDeliveryCount);
        Assert.NotNull(saved.Code);
        Assert.DoesNotContain("123456", saved.Code!.Hash);
        Assert.Equal("cipher", saved.CustomerCodeCipher);
    }

    [Fact]
    public async Task SaveUser_DuplicateEmail_ViolatesUniqueIndex()
    {
        using var host = new TestHost();
        using var scope = host.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Add(User.Create("dup@x.com", "h", "A", Role.Customer));
        db.Add(User.Create("dup@x.com", "h", "B", Role.Customer));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
