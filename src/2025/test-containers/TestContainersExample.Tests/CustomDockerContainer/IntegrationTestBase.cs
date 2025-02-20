using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestContainersExample.API;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace TestContainersExample.Tests.CustomDockerContainer;

public class IntegrationTestBase : IAsyncLifetime
{
    protected HttpClient _client;
    private IContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory;

    public async Task InitializeAsync()
    {
        var postgresImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "TestContainersExample.Tests/CustomDockerContainer")
            .WithDockerfile("Dockerfile")
            .WithName("custom-postgres")
            .Build();
        await postgresImage.CreateAsync();

        _postgresContainer = new ContainerBuilder()
            .WithImage(postgresImage)
            .WithExposedPort(5432)
            .WithEnvironment("POSTGRES_DB", "testdb")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "testpassword")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _postgresContainer.StartAsync();
        var connectionString = $"Host={_postgresContainer.Hostname};Port=5432;Database=testdb;Username=postgres;Password=testpassword";

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
