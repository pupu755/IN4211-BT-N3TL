using FoodHub.Domain.Entities;
using FoodHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FoodHub.Infrastructure.Persistence.Configurations;

internal static class MoneyConversion
{
    public static readonly ValueConverter<Money, decimal> Converter =
        new(money => money.Amount, amount => Money.Of(amount));
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TokenHash).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> b)
    {
        b.ToTable("Restaurants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Address).HasMaxLength(200).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.MenuItems).WithOne().HasForeignKey(m => m.RestaurantId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.MenuItems).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.ToTable("MenuItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Price).HasConversion(MoneyConversion.Converter).HasColumnType("numeric(18,2)");
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CouponCode).HasMaxLength(50);
        b.Property(x => x.DiscountAmount).HasConversion(MoneyConversion.Converter).HasColumnType("numeric(18,2)");
        b.Ignore(x => x.Total);
        b.Ignore(x => x.FinalAmount);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Restaurant>().WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.CustomerId);
        b.HasIndex(x => x.RestaurantId);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.UnitPrice).HasConversion(MoneyConversion.Converter).HasColumnType("numeric(18,2)");
        b.Ignore(x => x.LineTotal);
    }
}

internal sealed class ShipperProfileConfiguration : IEntityTypeConfiguration<ShipperProfile>
{
    public void Configure(EntityTypeBuilder<ShipperProfile> b)
    {
        b.ToTable("ShipperProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.VehiclePlate).HasMaxLength(20).IsRequired();
        b.Ignore(x => x.ActiveDeliveryCount);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasMany(x => x.Deliveries).WithOne().HasForeignKey(d => d.ShipperProfileId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Deliveries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> b)
    {
        b.ToTable("Deliveries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Ignore(x => x.IsCompleted);
        b.Property(x => x.CustomerCodeCipher).HasMaxLength(200);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.OrderId).IsUnique();
        b.OwnsOne(x => x.Code, code =>
        {
            code.Property(c => c.Salt).HasColumnName("CodeSalt").HasMaxLength(50);
            code.Property(c => c.Hash).HasColumnName("CodeHash").HasMaxLength(100);
            code.Property(c => c.ExpiresAtUtc).HasColumnName("CodeExpiresAtUtc");
            code.Property(c => c.FailedAttempts).HasColumnName("CodeFailedAttempts");
            code.Property(c => c.IsUsed).HasColumnName("CodeIsUsed");
            code.Ignore(c => c.IsLocked);
        });
    }
}

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Comment).HasMaxLength(500);
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasIndex(x => x.RestaurantId);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Restaurant>().WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.ToTable("Coupons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.DiscountAmount).HasConversion(MoneyConversion.Converter).HasColumnType("numeric(18,2)");
        b.Property(x => x.MinOrderAmount).HasConversion(MoneyConversion.Converter).HasColumnType("numeric(18,2)");
    }
}
