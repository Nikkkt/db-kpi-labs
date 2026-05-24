using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using lab3.Domain.Entities;

namespace lab3.Infrastructure.Repositories
{
    public interface IWeatherRepository
    {
        Task AddRangeAsync(IEnumerable<WeatherRecord> records);
        Task<List<WeatherRecord>> GetByCountryAndDateAsync(string country, DateTime date, string? location = null);
        Task<int> SaveChangesAsync();
        Task BulkFillIsGoodToGoOutsideAsync();
    }

    public class WeatherRepository : IWeatherRepository
    {
        private readonly Data.WeatherDbContext _db;

        public WeatherRepository(Data.WeatherDbContext db) => _db = db;

        public async Task AddRangeAsync(IEnumerable<WeatherRecord> records)
        {
            await _db.WeatherRecords.AddRangeAsync(records);
        }

        public async Task<List<WeatherRecord>> GetByCountryAndDateAsync(
            string country, DateTime date, string? location = null)
        {
            var query = _db.WeatherRecords
                .Include(r => r.WindData)
                .Where(r => r.Country.ToLower() == country.ToLower()
                         && r.LastUpdated.Date == date.Date);

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(r => r.Location.ToLower().Contains(location.ToLower()));

            return await query.ToListAsync();
        }

        public async Task BulkFillIsGoodToGoOutsideAsync()
        {
            var windEntries = await _db.WindData.ToListAsync();
            foreach (var w in windEntries)
                w.IsGoodToGoOutside = w.CalculateIsGoodToGoOutside();
            await _db.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    }
}
