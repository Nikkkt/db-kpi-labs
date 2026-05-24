using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using lab3.Application.DTOs;
using lab3.Application.Services;
using lab3.Infrastructure.Data;
using lab3.Infrastructure.Repositories;

namespace lab3.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            string connStr = "Host=localhost;Port=5433;Database=weather_db;Username=postgres;Password=dblabspass;";
            for (int i = 0; i < args.Length - 1; i++)if (args[i] == "--conn") connStr = args[i + 1];

            services.AddDbContext<WeatherDbContext>(opt => opt.UseNpgsql(connStr));

            services.AddScoped<IWeatherRepository, WeatherRepository>();
            services.AddSingleton<WeatherCsvParser>();
            services.AddScoped<WeatherService>();

            var provider = services.BuildServiceProvider();

            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();

            Console.WriteLine("[DB] Підключення до PostgreSQL...");
            await db.Database.MigrateAsync();
            Console.WriteLine("[DB] Міграції застосовано успішно.");

            var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();

            await RunMenuAsync(weatherService);
        }

        static async Task RunMenuAsync(WeatherService service)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Weather Migration");
                Console.WriteLine("1. Імпорт CSV → БД");
                Console.WriteLine("2. Пошук погоди");
                Console.WriteLine("3. Перерахувати IsGoodToGo");
                Console.WriteLine("0. Вийти");
                Console.Write("Оберіть: ");

                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        await ImportMenu(service);
                        break;
                    case "2":
                        await SearchMenu(service);
                        break;
                    case "3":
                        await service.RecalculateGoOutsideAsync();
                        Console.WriteLine("Перераховано для всіх записів");
                        break;
                    case "0":
                        Console.WriteLine("До побачення!");
                        return;
                    default:
                        Console.WriteLine("Невірний вибір");
                        break;
                }
            }
        }

        static async Task ImportMenu(WeatherService service)
        {
            Console.Write("Шлях до CSV файлу: ");
            var path = Console.ReadLine()?.Trim() ?? "";
            if (!File.Exists(path))
            {
                Console.WriteLine("Файл не знайдено");
                return;
            }
            await service.ImportFromCsvAsync(path);
        }

        static async Task SearchMenu(WeatherService service)
        {
            Console.Write("Країна (наприклад: Ukraine): ");
            var country = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Дата (наприклад: 2024-01-15): ");
            if (!DateTime.TryParse(Console.ReadLine(), out var date))
            {
                Console.WriteLine("Невірний формат дати");
                return;
            }

            Console.Write("Місто (Enter щоб пропустити): ");
            var location = Console.ReadLine()?.Trim();

            var results = await service.GetWeatherAsync(country, date, location);

            if (results.Count == 0)
            {
                Console.WriteLine("Записи не знайдено");
                return;
            }

            Console.WriteLine($"\nЗнайдено {results.Count} запис(ів)");
            foreach (var r in results) PrintWeather(r);
        }

        static void PrintWeather(WeatherSummaryDto r)
        {
            string goOut = r.IsGoodToGoOutside.HasValue
                ? (r.IsGoodToGoOutside.Value ? "ТАК" : "НІ")
                : "Невідомо";

            Console.WriteLine();
            Console.WriteLine($"┌─ {r.Location}, {r.Country} [{r.LastUpdated:yyyy-MM-dd HH:mm}]");
            Console.WriteLine($"│  Стан: {r.Condition}");
            Console.WriteLine($"│  Температура: {r.TempC:F1}°C (відчувається {r.FeelsLikeC:F1}°C), Вологість: {r.Humidity}%");
            Console.WriteLine($"│  Вітер: {r.WindKph:F1} км/год ({r.WindMph:F1} mph), {r.WindDirection} ({r.WindDegree}°)");
            Console.WriteLine($"│  Пориви: {r.GustKph:F1} км/год ({r.GustMph:F1} mph)");
            Console.WriteLine($"│  Схід: {FormatTime(r.Sunrise)}, Захід: {FormatTime(r.Sunset)}");
            Console.WriteLine($"└  Виходити на вулицю: {goOut}");
        }

        static string FormatTime(TimeSpan ts)
        {
            var dt = DateTime.Today.Add(ts);
            return dt.ToString("h:mm tt");
        }
    }
}
