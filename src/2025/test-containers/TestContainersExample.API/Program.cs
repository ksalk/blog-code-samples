using Microsoft.EntityFrameworkCore;

namespace TestContainersExample.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureServices(builder.Services);

            var app = builder.Build();

            Configure(app);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenApi();
            services.AddDbContext<WeatherDbContext>(options =>
            {
                options.UseNpgsql("connection-string");
            });
            services.AddScoped<WeatherService>();
        }

        private static void Configure(WebApplication app)
        {
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
        }
    }
}