using FoodHub.Application.Interfaces;
using FoodHub.Domain.Interfaces;
using FoodHub.Infrastructure.Persistence;
using FoodHub.Infrastructure.Repositories;
using FoodHub.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers everything including the PostgreSQL DbContext.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing environment variable ConnectionStrings__Default.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        return services.AddInfrastructureServices(configuration);
    }

    /// <summary>Registers repositories and security services. The caller registers the DbContext.</summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = new JwtOptions
        {
            Key = configuration["Jwt:Key"] ?? string.Empty,
            Issuer = configuration["Jwt:Issuer"] ?? string.Empty,
            Audience = configuration["Jwt:Audience"] ?? string.Empty
        };
        if (int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes))
        {
            jwt.AccessTokenMinutes = minutes;
        }

        if (int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days))
        {
            jwt.RefreshTokenDays = days;
        }

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt__Key must be set and at least 32 characters long.");
        }

        services.AddSingleton(jwt);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ICodeProtector, AesGcmCodeProtector>();
        services.AddSingleton<IDeliveryCodeFactory, RandomDeliveryCodeFactory>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IShipperRepository, ShipperRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        return services;
    }
}
