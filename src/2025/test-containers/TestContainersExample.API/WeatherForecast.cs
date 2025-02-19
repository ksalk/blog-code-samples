namespace TestContainersExample.API;

public class WeatherForecast
{
    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int TemperatureC { get; private set; }
    public string Summary { get; private set; }

    public WeatherForecast(DateOnly date, int temperatureC, string summary)
    {
        Date = date;
        TemperatureC = temperatureC;
        Summary = summary;
    }
}
