using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using TestContainersExample.API;
using DotNet.Testcontainers.Builders;

namespace TestContainersExample.Tests;

public class PreconfiguredContainerIntegrationTestBase : IAsyncLifetime
{
    protected HttpClient _client;
    private PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory;

    private readonly string dbUsername = "testuser";
    private readonly string dbPassword = "testpassword";
    private readonly string dbName = "testdb";

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithUsername(dbUsername)
            .WithPassword(dbPassword)
            .WithDatabase(dbName)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _postgresContainer.StartAsync();
        var connectionString = _postgresContainer.GetConnectionString();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WeatherDbContext>));
                    if(descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<WeatherDbContext>(options =>
                    {
                        options.UseNpgsql(connectionString);
                    });
                });
            });
        
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
    }

    public void SeedWeatherEntities(params WeatherForecast[] weatherForecasts)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        context.WeatherForecasts.AddRange(weatherForecasts);
        context.SaveChanges();
    }
}
