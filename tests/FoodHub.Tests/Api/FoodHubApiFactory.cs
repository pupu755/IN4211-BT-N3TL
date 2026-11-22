using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodHub.Application.Dtos;
using FoodHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHub.Tests.Api;

/// <summary>Starts the real API in memory on SQLite. No Docker and no PostgreSQL needed.</summary>
public sealed class FoodHubApiFactory : WebApplicationFactory<Program>
{
    public const string SeedPassword = "Test#Pass123";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FoodHubApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", "Host=unused;Database=unused");
        builder.UseSetting("Jwt:Key", "api-test-key-api-test-key-api-test-key-12");
        builder.UseSetting("Jwt:Issuer", "FoodHub.Api.Tests");
        builder.UseSetting("Jwt:Audience", "FoodHub.Api.Tests.Clients");
        builder.UseSetting("Security:CodeKey", "api-test-code-key");
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                    || (d.ServiceType.IsGenericType && d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext))))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<FoodHub.Application.Interfaces.IPasswordHasher>(), SeedPassword)
                .GetAwaiter().GetResult();
        });
    }

    public async Task<HttpClient> CreateClientForAsync(string email, string password = SeedPassword)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
