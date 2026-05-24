using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace lab3.Infrastructure.Migrations
{
    /// <summary>
    /// Migration V2: Рефакторинг — виносимо дані вітру в окрему таблицю wind_data.
    /// Дані НЕ втрачаються — переносяться через SQL INSERT INTO ... SELECT.
    /// </summary>
    public partial class V2_ExtractWindTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Створити нову таблицю wind_data
            migrationBuilder.CreateTable(
                name: "wind_data",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WindKph       = table.Column<double>(type: "double precision", nullable: false),
                    WindMph       = table.Column<double>(type: "double precision", nullable: false),
                    WindDegree    = table.Column<int>(nullable: false),
                    WindDirection = table.Column<string>(maxLength: 10, nullable: false, defaultValue: "Unknown"),
                    GustKph       = table.Column<double>(type: "double precision", nullable: false),
                    GustMph       = table.Column<double>(type: "double precision", nullable: false),
                    Sunrise       = table.Column<string>(maxLength: 10, nullable: false, defaultValue: "00:00"),
                    Sunset        = table.Column<string>(maxLength: 10, nullable: false, defaultValue: "00:00"),
                    // Нова колонка булеан — IsGoodToGoOutside (без дефолтного значення, nullable)
                    IsGoodToGoOutside = table.Column<bool>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wind_data", x => x.Id);
                });

            // 2. Перенести дані з weather_records → wind_data
            //    Один запис wind_data на кожен weather_record
            migrationBuilder.Sql(@"
                INSERT INTO wind_data (
                    ""WindKph"", ""WindMph"", ""WindDegree"", ""WindDirection"",
                    ""GustKph"", ""GustMph"", ""Sunrise"", ""Sunset"", ""IsGoodToGoOutside""
                )
                SELECT
                    COALESCE(""WindKph"", 0),
                    COALESCE(""WindMph"", 0),
                    COALESCE(""WindDegree"", 0),
                    COALESCE(""WindDirection"", 'Unknown'),
                    COALESCE(""GustKph"", 0),
                    COALESCE(""GustMph"", 0),
                    COALESCE(""Sunrise"", '00:00'),
                    COALESCE(""Sunset"", '00:00'),
                    -- Обчислюємо IsGoodToGoOutside: вітер < 30 км/год і пориви < 50 км/год
                    CASE WHEN COALESCE(""WindKph"", 0) < 30 AND COALESCE(""GustKph"", 0) < 50
                         THEN TRUE ELSE FALSE END
                FROM weather_records;
            ");

            // 3. Додати колонку WindDataId у weather_records
            migrationBuilder.AddColumn<int>(
                name: "WindDataId",
                table: "weather_records",
                nullable: true);

            // 4. Зв'язати записи (weather_records.id = wind_data.id через rownum)
            //    Оскільки ми вставляли в тому ж порядку, ID збігаються 1:1
            migrationBuilder.Sql(@"
                UPDATE weather_records w
                SET ""WindDataId"" = wd.""Id""
                FROM wind_data wd
                WHERE wd.""Id"" = w.""Id"";
            ");

            // 5. Додати foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_weather_records_wind_data_WindDataId",
                table: "weather_records",
                column: "WindDataId",
                principalTable: "wind_data",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateIndex(
                name: "IX_weather_records_WindDataId",
                table: "weather_records",
                column: "WindDataId",
                unique: true);

            // 6. Видалити старі колонки вітру з weather_records
            migrationBuilder.DropColumn("WindKph",       "weather_records");
            migrationBuilder.DropColumn("WindMph",       "weather_records");
            migrationBuilder.DropColumn("WindDegree",    "weather_records");
            migrationBuilder.DropColumn("WindDirection", "weather_records");
            migrationBuilder.DropColumn("GustKph",       "weather_records");
            migrationBuilder.DropColumn("GustMph",       "weather_records");
            migrationBuilder.DropColumn("Sunrise",       "weather_records");
            migrationBuilder.DropColumn("Sunset",        "weather_records");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Відкат: повертаємо колонки, переносимо дані назад, видаляємо wind_data

            migrationBuilder.AddColumn<double>("WindKph",    "weather_records", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<double>("WindMph",    "weather_records", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<int>   ("WindDegree", "weather_records", nullable: true);
            migrationBuilder.AddColumn<string>("WindDirection","weather_records", maxLength: 10, nullable: true);
            migrationBuilder.AddColumn<double>("GustKph",   "weather_records", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<double>("GustMph",   "weather_records", type: "double precision", nullable: true);
            migrationBuilder.AddColumn<string>("Sunrise",   "weather_records", maxLength: 10, nullable: true);
            migrationBuilder.AddColumn<string>("Sunset",    "weather_records", maxLength: 10, nullable: true);

            migrationBuilder.Sql(@"
                UPDATE weather_records w
                SET ""WindKph""       = wd.""WindKph"",
                    ""WindMph""       = wd.""WindMph"",
                    ""WindDegree""    = wd.""WindDegree"",
                    ""WindDirection"" = wd.""WindDirection"",
                    ""GustKph""       = wd.""GustKph"",
                    ""GustMph""       = wd.""GustMph"",
                    ""Sunrise""       = wd.""Sunrise"",
                    ""Sunset""        = wd.""Sunset""
                FROM wind_data wd
                WHERE wd.""Id"" = w.""WindDataId"";
            ");

            migrationBuilder.DropForeignKey("FK_weather_records_wind_data_WindDataId", "weather_records");
            migrationBuilder.DropIndex("IX_weather_records_WindDataId", "weather_records");
            migrationBuilder.DropColumn("WindDataId", "weather_records");
            migrationBuilder.DropTable("wind_data");
        }
    }
}
