using System;

namespace lab3.Domain.Entities
{
    public class WeatherRecord
    {
        public int Id { get; set; }

        public string Country { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;

        public DateTime LastUpdated { get; set; }

        public double TempC { get; set; }
        public double FeelsLikeC { get; set; }
        public int Humidity { get; set; }

        public int? WindDataId { get; set; }
        public WindData? WindData { get; set; }
    }
}
