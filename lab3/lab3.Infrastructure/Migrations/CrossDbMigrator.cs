using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using lab3.Infrastructure.Data;

namespace lab3.Infrastructure.Migrations
{
    /// <summary>
    /// Інструмент для міграції даних між PostgreSQL та MySQL.
    /// Генерує SQL-скрипти INSERT для перенесення даних.
    ///
    /// Процес:
    ///   1. Підключаємось до джерела (Postgres або MySQL)
    ///   2. Генеруємо INSERT скрипти
    ///   3. Застосовуємо скрипти на цільовій БД
    /// </summary>
    public class CrossDbMigrator
    {
        private readonly WeatherDbContext _source;

        public CrossDbMigrator(WeatherDbContext source) => _source = source;

        /// <summary>
        /// Генерує SQL файл з INSERT-ами, сумісними з MySQL та PostgreSQL.
        /// </summary>
        public async Task ExportToSqlFileAsync(string outputPath)
        {
            Console.WriteLine("[Export] Читання даних з БД...");
            var windData  = await _source.WindData.ToListAsync();
            var records   = await _source.WeatherRecords.ToListAsync();

            Console.WriteLine($"[Export] wind_data: {windData.Count} записів");
            Console.WriteLine($"[Export] weather_records: {records.Count} записів");

            var sb = new StringBuilder();
            sb.AppendLine("-- Автоматично згенерований скрипт міграції даних");
            sb.AppendLine($"-- Дата: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();
            sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;  -- MySQL");
            sb.AppendLine("-- SET session_replication_role = 'replica'; -- PostgreSQL");
            sb.AppendLine();

            // wind_data
            sb.AppendLine("-- ── wind_data ─────────────────────────────────────────");
            foreach (var w in windData)
            {
                string goOut = w.IsGoodToGoOutside.HasValue
                    ? (w.IsGoodToGoOutside.Value ? "1" : "0")
                    : "NULL";

                sb.AppendLine(
                    $"INSERT INTO wind_data (Id, WindKph, WindMph, WindDegree, WindDirection, " +
                    $"GustKph, GustMph, Sunrise, Sunset, IsGoodToGoOutside) VALUES " +
                    $"({w.Id}, {w.WindKph.ToSql()}, {w.WindMph.ToSql()}, {w.WindDegree}, " +
                    $"'{Escape(w.WindDirection.ToString())}', " +
                    $"{w.GustKph.ToSql()}, {w.GustMph.ToSql()}, " +
                    $"'{w.Sunrise:hh\\:mm}', '{w.Sunset:hh\\:mm}', {goOut});");
            }

            sb.AppendLine();
            sb.AppendLine("-- ── weather_records ────────────────────────────────────");
            foreach (var r in records)
            {
                string windId = r.WindDataId.HasValue ? r.WindDataId.ToString()! : "NULL";
                sb.AppendLine(
                    $"INSERT INTO weather_records (Id, Country, Location, Condition, LastUpdated, " +
                    $"TempC, FeelsLikeC, Humidity, WindDataId) VALUES " +
                    $"({r.Id}, '{Escape(r.Country)}', '{Escape(r.Location)}', " +
                    $"'{Escape(r.Condition)}', '{r.LastUpdated:yyyy-MM-dd HH:mm:ss}', " +
                    $"{r.TempC.ToSql()}, {r.FeelsLikeC.ToSql()}, {r.Humidity}, {windId});");
            }

            sb.AppendLine();
            sb.AppendLine("SET FOREIGN_KEY_CHECKS = 1;  -- MySQL");
            sb.AppendLine("-- SET session_replication_role = 'DEFAULT'; -- PostgreSQL");

            await File.WriteAllTextAsync(outputPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"[Export] Скрипт збережено: {outputPath}");
        }

        private static string Escape(string s) => s?.Replace("'", "''") ?? "";
    }

    internal static class SqlExtensions
    {
        public static string ToSql(this double v)
            => v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
    }
}
