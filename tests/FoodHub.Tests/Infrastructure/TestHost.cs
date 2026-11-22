using FoodHub.Application;
using FoodHub.Infrastructure;
using FoodHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Tests.Infrastructure;

/// <summary>Builds the real application and infrastructure services on top of an in-memory SQLite database.</summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestHost()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-key-test-key-test-key-test-key-1234",
                ["Jwt:Issuer"] = "FoodHub.Tests",
                ["Jwt:Audience"] = "FoodHub.Tests.Clients",
                ["Security:CodeKey"] = "test-code-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddInfrastructureServices(configuration);
        services.AddApplication();
        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public IServiceProvider Services { get; }

    public IServiceScope NewScope() => Services.CreateScope();

    public void Dispose()
    {
        (Services as IDisposable)?.Dispose();
        _connection.Dispose();
    }
}
