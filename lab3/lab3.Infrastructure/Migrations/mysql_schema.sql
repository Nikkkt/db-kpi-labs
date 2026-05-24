-- ============================================================
-- MySQL-сумісний скрипт міграції (V1 + V2 за один раз)
-- Запускається при переході PostgreSQL → MySQL
-- ============================================================

-- V1: Початкова схема
CREATE TABLE IF NOT EXISTS weather_records (
    Id          INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Country     VARCHAR(100) NOT NULL,
    Location    VARCHAR(200) NOT NULL,
    Condition   VARCHAR(200),
    LastUpdated DATETIME     NOT NULL,
    TempC       DOUBLE       NOT NULL DEFAULT 0,
    FeelsLikeC  DOUBLE       NOT NULL DEFAULT 0,
    Humidity    INT          NOT NULL DEFAULT 0,
    WindDataId  INT          NULL,

    INDEX IX_weather_records_Country_LastUpdated (Country, LastUpdated)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- V2: Окрема таблиця вітру
CREATE TABLE IF NOT EXISTS wind_data (
    Id                INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    WindKph           DOUBLE       NOT NULL DEFAULT 0,
    WindMph           DOUBLE       NOT NULL DEFAULT 0,
    WindDegree        INT          NOT NULL DEFAULT 0,
    WindDirection     VARCHAR(10)  NOT NULL DEFAULT 'Unknown',
    GustKph           DOUBLE       NOT NULL DEFAULT 0,
    GustMph           DOUBLE       NOT NULL DEFAULT 0,
    Sunrise           VARCHAR(10)  NOT NULL DEFAULT '00:00',
    Sunset            VARCHAR(10)  NOT NULL DEFAULT '00:00',
    IsGoodToGoOutside TINYINT(1)   NULL     -- NULL = не обчислено
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Foreign key
ALTER TABLE weather_records
    ADD CONSTRAINT FK_weather_records_wind_data_WindDataId
    FOREIGN KEY (WindDataId) REFERENCES wind_data(Id)
    ON DELETE SET NULL;

-- Індекс на FK
CREATE UNIQUE INDEX IX_weather_records_WindDataId
    ON weather_records (WindDataId);
