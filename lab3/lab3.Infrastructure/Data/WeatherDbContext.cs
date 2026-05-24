using Microsoft.EntityFrameworkCore;
using lab3.Domain.Entities;
using lab3.Domain.Enums;

namespace lab3.Infrastructure.Data
{
    public class WeatherDbContext : DbContext
    {
        public DbSet<WeatherRecord> WeatherRecords { get; set; } = null!;
        public DbSet<WindData> WindData { get; set; } = null!;

        public WeatherDbContext(DbContextOptions<WeatherDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WeatherRecord>(entity =>
            {
                entity.ToTable("weather_records");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Country)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Location)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Condition)
                    .HasMaxLength(200);

                entity.Property(e => e.LastUpdated)
                    .IsRequired()
                    .HasColumnType("timestamp without time zone");

                entity.Property(e => e.TempC)
                    .HasColumnType("double precision");

                entity.Property(e => e.FeelsLikeC)
                    .HasColumnType("double precision");

                entity.HasOne(e => e.WindData)
                    .WithOne(w => w.WeatherRecord)
                    .HasForeignKey<WeatherRecord>(e => e.WindDataId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<WindData>(entity =>
            {
                entity.ToTable("wind_data");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.WindKph)
                    .HasColumnType("double precision");

                entity.Property(e => e.WindMph)
                    .HasColumnType("double precision");

                entity.Property(e => e.WindDegree)
                    .IsRequired();

                entity.Property(e => e.WindDirection)
                    .HasConversion<string>()
                    .HasMaxLength(10);

                entity.Property(e => e.GustKph)
                    .HasColumnType("double precision");

                entity.Property(e => e.GustMph)
                    .HasColumnType("double precision");

                entity.Property(e => e.Sunrise)
                    .HasConversion(
                        ts => ts.ToString(@"hh\:mm"),
                        s => TimeSpan.Parse(s)
                    )
                    .HasMaxLength(10);

                entity.Property(e => e.Sunset)
                    .HasConversion(
                        ts => ts.ToString(@"hh\:mm"),
                        s => TimeSpan.Parse(s)
                    )
                    .HasMaxLength(10);

                entity.Property(e => e.IsGoodToGoOutside)
                    .IsRequired(false);
            });
        }
    }
}
