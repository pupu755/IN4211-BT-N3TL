using FluentValidation;
using FoodHub.Application.Interfaces;
using FoodHub.Application.Services;
using FoodHub.Application.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

        services.AddScoped<AuthService>();
        services.AddScoped<RestaurantService>();
        services.AddScoped<OrderService>();
        services.AddScoped<DeliveryService>();

        services.AddSingleton<IDeliveryFeeStrategy>(_ => new FreeShippingOverThresholdStrategy());
        return services;
    }
}
