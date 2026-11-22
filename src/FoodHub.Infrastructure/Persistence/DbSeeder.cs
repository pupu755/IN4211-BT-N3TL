using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;
using FoodHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence;

/// <summary>Creates one sample account per role and one sample restaurant. Safe to run more than once.</summary>
public static class DbSeeder
{
    public static readonly string[] SampleEmails =
    {
        "admin@foodhub.local",
        "customer@foodhub.local",
        "restaurant@foodhub.local",
        "shipper@foodhub.local"
    };

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, string password, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var passwordHash = hasher.Hash(password);
        var admin = User.Create("admin@foodhub.local", passwordHash, "Sample Admin", Role.Admin);
        var customer = User.Create("customer@foodhub.local", passwordHash, "Sample Customer", Role.Customer);
        var owner = User.Create("restaurant@foodhub.local", passwordHash, "Sample Restaurant Owner", Role.Restaurant);
        var shipperUser = User.Create("shipper@foodhub.local", passwordHash, "Sample Shipper", Role.Shipper);
        var shipper = ShipperProfile.Create(shipperUser.Id, "59A1-00000");

        var restaurant = Restaurant.Create(owner.Id, "Sample Pho 24", "1 Le Loi, District 1");
        restaurant.AddMenuItem("Pho bo", Money.Of(50000m));
        restaurant.AddMenuItem("Tra da", Money.Of(10000m));

        db.AddRange(admin, customer, owner, shipperUser, shipper, restaurant);
        await db.SaveChangesAsync(ct);
    }
}
