using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using lab3.Application.DTOs;
using lab3.Domain.Entities;
using lab3.Infrastructure.Data;
using lab3.Infrastructure.Repositories;

namespace lab3.Application.Services
{
    public class WeatherService
    {
        private readonly IWeatherRepository _repo;
        private readonly WeatherCsvParser _parser;

        public WeatherService(IWeatherRepository repo, WeatherCsvParser parser)
        {
            _repo   = repo;
            _parser = parser;
        }

        public async Task ImportFromCsvAsync(string csvPath, int batchSize = 500)
        {
            Console.WriteLine($"Парсинг файлу: {csvPath}");
            var rows = _parser.Parse(csvPath);
            Console.WriteLine($"Зчитано {rows.Count} записів");

            int total = 0;
            for (int i = 0; i < rows.Count; i += batchSize)
            {
                var batch = rows.Skip(i).Take(batchSize)
                    .Select(t => t.Record)
                    .ToList();

                await _repo.AddRangeAsync(batch);
                await _repo.SaveChangesAsync();
                total += batch.Count;
                Console.WriteLine($"Збережено {total}/{rows.Count}");
            }

            Console.WriteLine("Завершено");
        }

        public async Task<List<WeatherSummaryDto>> GetWeatherAsync(
            string country, DateTime date, string? location = null)
        {
            var records = await _repo.GetByCountryAndDateAsync(country, date, location);
            return records.Select(MapToDto).ToList();
        }

        public Task RecalculateGoOutsideAsync() => _repo.BulkFillIsGoodToGoOutsideAsync();

        private static WeatherSummaryDto MapToDto(WeatherRecord r)
        {
            var w = r.WindData;
            return new WeatherSummaryDto
            {
                Country        = r.Country,
                Location       = r.Location,
                Condition      = r.Condition,
                LastUpdated    = r.LastUpdated,
                TempC          = r.TempC,
                FeelsLikeC     = r.FeelsLikeC,
                Humidity       = r.Humidity,
                WindKph        = w?.WindKph        ?? 0,
                WindMph        = w?.WindMph        ?? 0,
                WindDegree     = w?.WindDegree     ?? 0,
                WindDirection  = w?.WindDirection  ?? Domain.Enums.WindDirection.Unknown,
                GustKph        = w?.GustKph        ?? 0,
                GustMph        = w?.GustMph        ?? 0,
                Sunrise        = w?.Sunrise        ?? TimeSpan.Zero,
                Sunset         = w?.Sunset         ?? TimeSpan.Zero,
                IsGoodToGoOutside = w?.IsGoodToGoOutside,
            };
        }
    }
}
