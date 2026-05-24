using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace lab3.Infrastructure.Migrations
{
    /// <summary>
    /// Migration V1: Створення початкової схеми.
    /// Одна таблиця weather_records з усіма полями.
    /// </summary>
    public partial class V1_InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Таблиця weather_records (моноліт, до рефакторингу) ──
            migrationBuilder.CreateTable(
                name: "weather_records",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Country     = table.Column<string>(maxLength: 100, nullable: false),
                    Location    = table.Column<string>(maxLength: 200, nullable: false),
                    Condition   = table.Column<string>(maxLength: 200, nullable: true),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    TempC       = table.Column<double>(type: "double precision", nullable: false),
                    FeelsLikeC  = table.Column<double>(type: "double precision", nullable: false),
                    Humidity    = table.Column<int>(nullable: false),

                    // Поля вітру — тимчасово тут, до міграції V2
                    WindKph       = table.Column<double>(type: "double precision", nullable: true),
                    WindMph       = table.Column<double>(type: "double precision", nullable: true),
                    WindDegree    = table.Column<int>(nullable: true),
                    WindDirection = table.Column<string>(maxLength: 10, nullable: true),
                    GustKph       = table.Column<double>(type: "double precision", nullable: true),
                    GustMph       = table.Column<double>(type: "double precision", nullable: true),
                    Sunrise       = table.Column<string>(maxLength: 10, nullable: true),
                    Sunset        = table.Column<string>(maxLength: 10, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weather_records_Country_LastUpdated",
                table: "weather_records",
                columns: new[] { "Country", "LastUpdated" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "weather_records");
        }
    }
}
