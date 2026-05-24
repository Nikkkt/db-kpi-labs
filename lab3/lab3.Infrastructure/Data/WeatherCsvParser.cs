using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using lab3.Domain.Entities;
using lab3.Domain.Enums;

namespace lab3.Infrastructure.Data
{
    public class WeatherCsvParser
    {
        private readonly Dictionary<string, int> _columnIndex = new();

        public List<(WeatherRecord Record, WindData Wind)> Parse(string csvPath)
        {
            var results = new List<(WeatherRecord, WindData)>();

            using var reader = new StreamReader(csvPath);
            var headerLine = reader.ReadLine();
            if (headerLine == null) return results;

            var headers = headerLine.Split(',');
            for (int i = 0; i < headers.Length; i++)
                _columnIndex[headers[i].Trim().ToLower()] = i;

            string? line;
            int lineNum = 1;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                try
                {
                    var cols = SplitCsvLine(line);
                    var (record, wind) = ParseRow(cols);
                    results.Add((record, wind));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[WARN] Рядок {lineNum} пропущено: {ex.Message}");
                }
            }

            return results;
        }

        private (WeatherRecord, WindData) ParseRow(string[] cols)
        {
            var wind = new WindData
            {
                WindKph    = GetDouble(cols, "wind_kph"),
                WindMph    = GetDouble(cols, "wind_mph"),
                WindDegree = GetInt(cols, "wind_degree"),
                WindDirection = ParseWindDirection(GetString(cols, "wind_direction")),
                GustKph    = GetDouble(cols, "gust_kph"),
                GustMph    = GetDouble(cols, "gust_mph"),
                Sunrise    = ParseTime(GetString(cols, "sunrise")),
                Sunset     = ParseTime(GetString(cols, "sunset")),
            };
            wind.IsGoodToGoOutside = wind.CalculateIsGoodToGoOutside();

            var record = new WeatherRecord
            {
                Country     = GetString(cols, "country"),
                Location    = GetString(cols, "location_name"),
                Condition   = GetString(cols, "condition_text"),
                LastUpdated = ParseDateTime(GetString(cols, "last_updated")),
                TempC       = GetDouble(cols, "temperature_celsius"),
                FeelsLikeC  = GetDouble(cols, "feels_like_celsius"),
                Humidity    = GetInt(cols, "humidity"),
                WindData    = wind,
            };

            return (record, wind);
        }

        private string GetString(string[] cols, string col)
        {
            if (!_columnIndex.TryGetValue(col, out int idx) || idx >= cols.Length)
                return string.Empty;
            return cols[idx].Trim().Trim('"');
        }

        private double GetDouble(string[] cols, string col)
        {
            var s = GetString(cols, col);
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private int GetInt(string[] cols, string col)
        {
            var s = GetString(cols, col);
            return int.TryParse(s, out var v) ? v : 0;
        }

        private static WindDirection ParseWindDirection(string raw)
        {
            return Enum.TryParse<WindDirection>(raw, ignoreCase: true, out var dir)
                ? dir
                : WindDirection.Unknown;
        }

        private static DateTime ParseDateTime(string raw)
        {
            if (DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            if (DateTime.TryParse(raw, out dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified);
        }

        private static TimeSpan ParseTime(string raw)
        {
            raw = raw.Trim();
            if (DateTime.TryParseExact(raw, new[] { "hh:mm tt", "h:mm tt", "HH:mm", "H:mm" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.TimeOfDay;
            return TimeSpan.Zero;
        }
        private static string[] SplitCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (char c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
