using Microsoft.EntityFrameworkCore;
using TestContainersExample.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<WeatherDbContext>(options =>
{
    options.UseNpgsql("connection-string");
});
builder.Services.AddScoped<WeatherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async (WeatherService weatherService) =>
{
    var weatherForecast = await weatherService.GetWeatherForecast();
    return Results.Ok(weatherForecast);
})
.WithName("GetWeatherForecast");

app.Run();