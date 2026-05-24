using System;
using lab3.Domain.Enums;

namespace lab3.Domain.Entities
{
    public class WindData
    {
        public int Id { get; set; }

        public double WindKph { get; set; }
        public double WindMph { get; set; }

        public int WindDegree { get; set; }

        public WindDirection WindDirection { get; set; }

        public double GustKph { get; set; }
        public double GustMph { get; set; }

        public TimeSpan Sunrise { get; set; }
        public TimeSpan Sunset { get; set; }

        public bool? IsGoodToGoOutside { get; set; }

        public WeatherRecord? WeatherRecord { get; set; }

        public bool CalculateIsGoodToGoOutside()
        {
            return WindKph < 30.0 && GustKph < 50.0;
        }
    }
}
