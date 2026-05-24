using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace lab3.Infrastructure.Data
{
    public class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
    {
        public WeatherDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5433;Database=weather_db;Username=postgres;Password=dblabspass;");

            return new WeatherDbContext(optionsBuilder.Options);
        }
    }
}