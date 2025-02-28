using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestContainersExample.API;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace TestContainersExample.Tests;

public class CustomContainerIntegrationTestBase : IAsyncLifetime
{
    protected HttpClient _client;
    private IContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory;

    private readonly string dbUsername = "testuser";
    private readonly string dbPassword = "testpassword";
    private readonly string dbName = "testdb";
    private static int port => 5432;

    public async Task InitializeAsync()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:latest")
            .WithPortBinding(port, true)
            .WithEnvironment("POSTGRES_USER", dbUsername)
            .WithEnvironment("POSTGRES_PASSWORD", dbPassword)
            .WithEnvironment("POSTGRES_DB", dbName)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(port))
            .Build();

        await _postgresContainer.StartAsync();
        var connectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(port)};Database={dbName};Username={dbUsername};Password={dbPassword}";

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
