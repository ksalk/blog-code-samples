using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestContainersExample.API;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace TestContainersExample.Tests.DockerfileContainer;

public class IntegrationTestBase : IAsyncLifetime
{
    protected HttpClient _client;
    private IContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory;

    private readonly string dbUsername = "testuser";
    private readonly string dbPassword = "testpassword";
    private readonly string dbName = "testdb";

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
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_USER", dbUsername)
            .WithEnvironment("POSTGRES_PASSWORD", dbPassword)
            .WithEnvironment("POSTGRES_DB", dbName)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _postgresContainer.StartAsync();
        var connectionString = $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};Database={dbName};Username={dbUsername};Password={dbPassword}";

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
