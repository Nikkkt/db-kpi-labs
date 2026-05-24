using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace lab3.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wind_data",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WindKph = table.Column<double>(type: "double precision", nullable: false),
                    WindMph = table.Column<double>(type: "double precision", nullable: false),
                    WindDegree = table.Column<int>(type: "integer", nullable: false),
                    WindDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    GustKph = table.Column<double>(type: "double precision", nullable: false),
                    GustMph = table.Column<double>(type: "double precision", nullable: false),
                    Sunrise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Sunset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsGoodToGoOutside = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wind_data", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weather_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TempC = table.Column<double>(type: "double precision", nullable: false),
                    FeelsLikeC = table.Column<double>(type: "double precision", nullable: false),
                    Humidity = table.Column<int>(type: "integer", nullable: false),
                    WindDataId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weather_records_wind_data_WindDataId",
                        column: x => x.WindDataId,
                        principalTable: "wind_data",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weather_records_WindDataId",
                table: "weather_records",
                column: "WindDataId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weather_records");

            migrationBuilder.DropTable(
                name: "wind_data");
        }
    }
}
