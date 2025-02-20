using System.Net.Http.Json;
using TestContainersExample.API;

namespace TestContainersExample.Tests.CustomDockerContainer;

public class WeatherEndpointIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task TestWeatherForecastEndpoint()
    {
        // Arrange
        var weatherForecast = new WeatherForecast(new DateOnly(2021, 1, 1), 25, "Sunny");
        SeedWeatherEntities(weatherForecast);

        // Act
        var response = await _client.GetAsync("/weatherforecast");

        // Assert
        response.EnsureSuccessStatusCode();

        var actualForecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        Assert.NotNull(actualForecasts);
        Assert.Single(actualForecasts);
        Assert.Equal(weatherForecast.Date, actualForecasts[0].Date);
        Assert.Equal(weatherForecast.TemperatureC, actualForecasts[0].TemperatureC);
        Assert.Equal(weatherForecast.Summary, actualForecasts[0].Summary);
    }
}