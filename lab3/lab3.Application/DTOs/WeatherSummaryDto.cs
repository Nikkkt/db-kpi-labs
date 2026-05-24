using System;
using lab3.Domain.Enums;

namespace lab3.Application.DTOs
{
    public class WeatherSummaryDto
    {
        public string Country { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }

        public double TempC { get; set; }
        public double FeelsLikeC { get; set; }
        public int Humidity { get; set; }

        public double WindKph { get; set; }
        public double WindMph { get; set; }
        public int WindDegree { get; set; }
        public WindDirection WindDirection { get; set; }
        public double GustKph { get; set; }
        public double GustMph { get; set; }

        public TimeSpan Sunrise { get; set; }
        public TimeSpan Sunset { get; set; }

        public bool? IsGoodToGoOutside { get; set; }
    }
}
