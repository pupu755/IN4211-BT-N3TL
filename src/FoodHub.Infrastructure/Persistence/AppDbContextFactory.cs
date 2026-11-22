using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodHub.Infrastructure.Persistence;

/// <summary>Used only by dotnet ef at design time. The connection string is a placeholder and never opened.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=foodhub_design;Username=design;Password=design")
            .Options;
        return new AppDbContext(options);
    }
}
