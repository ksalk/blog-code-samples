using Microsoft.EntityFrameworkCore;

namespace TestContainersExample.API;

public class WeatherService(WeatherDbContext dbContext)
{
    public async Task<WeatherForecast[]> GetWeatherForecast()
    {
        return await dbContext.WeatherForecasts.ToArrayAsync();
    }
}
