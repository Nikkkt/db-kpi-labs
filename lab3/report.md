# Лабораторна робота №3
## Міграція Бази Даних

**Виконав:** Терпіловський Нікіта  
**Мова програмування:** C# (.NET 10)  
**ORM:** Entity Framework Core 9  
**База даних:** PostgreSQL  
**Інструмент міграцій:** EF Core Migrations  
**Датасет:** [Global Weather Repository (Kaggle)](https://www.kaggle.com/datasets/nelgiriyewithana/global-weather-repository)  

---

## 1. Мета роботи

Розробити програму у стилі Layered Architecture з ORM-моделлю бази даних, реалізувати зчитування даних з CSV-файлу, виконати рефакторинг-міграцію схеми бази даних шляхом винесення даних вітру в окрему таблицю, а також забезпечити можливість пошуку погодних даних за країною та датою.

---

## 2. Архітектура застосунку

Проєкт реалізований за патерном **Layered Architecture** і складається з 4 шарів:

```
lab3/
├── lab3.Domain/            ← Шар домену
├── lab3.Infrastructure/    ← Шар інфраструктури
├── lab3.Application/       ← Шар застосунку
└── lab3.Console/           ← Шар представлення
```

### Опис шарів

**lab3.Domain** — містить сутності (`WeatherRecord`, `WindData`) та enum (`WindDirection`). Не залежить від жодного зовнішнього фреймворку.

**lab3.Infrastructure** — містить `WeatherDbContext` (ORM-конфігурація), `WeatherCsvParser` (зчитування файлу), `WeatherRepository` (доступ до даних), міграції та `WeatherDbContextFactory`.

**lab3.Application** — містить `WeatherService` (бізнес-логіка: імпорт, пошук, перерахунок) та DTO для передачі даних між шарами.

**lab3.Console** — точка входу. Консольний інтерфейс з меню, введенням даних підключення та відображенням результатів.

---

## 3. ORM-модель

Для опису структури бази даних використовується **Entity Framework Core** — ORM для .NET. Замість написання SQL вручну, структура таблиць описується C#-класами, а маппінг на колонки бази налаштовується в методі `OnModelCreating` контексту.

### 3.1 Таблиця `weather_records`

```csharp
public class WeatherRecord
{
    public int Id { get; set; }
    public string Country { get; set; }      // текстова
    public string Location { get; set; }     // текстова
    public string Condition { get; set; }    // текстова
    public DateTime LastUpdated { get; set; } // дата
    public double TempC { get; set; }        // дробне
    public double FeelsLikeC { get; set; }   // дробне
    public int Humidity { get; set; }        // ціле
    public int? WindDataId { get; set; }     // зовнішній ключ
    public WindData? WindData { get; set; }  // навігаційна властивість
}
```

### 3.2 Таблиця `wind_data` (варіант 1 — вітер)

```csharp
public class WindData
{
    public int Id { get; set; }
    public double WindKph { get; set; }           // дробне
    public double WindMph { get; set; }           // дробне
    public int WindDegree { get; set; }           // ціле
    public WindDirection WindDirection { get; set; } // enum
    public double GustKph { get; set; }           // дробне
    public double GustMph { get; set; }           // дробне
    public TimeSpan Sunrise { get; set; }         // час
    public TimeSpan Sunset { get; set; }          // час
    public bool? IsGoodToGoOutside { get; set; }  // булеан
}
```

### 3.3 Enum WindDirection

```csharp
public enum WindDirection
{
    N, NNE, NE, ENE, E, ESE, SE, SSE,
    S, SSW, SW, WSW, W, WNW, NW, NNW,
    Unknown
}
```

Enum зберігається в базі як рядок (`HasConversion<string>()`), що дозволяє читати значення на кшталт "NW" або "SSE" безпосередньо в базі без декодування.

### 3.4 Конфігурація в DbContext

Особливості конфігурації ORM:

```csharp
// Дата без часової зони — уникаємо конфлікту з Npgsql
entity.Property(e => e.LastUpdated)
    .HasColumnType("timestamp without time zone");

// Enum як рядок
entity.Property(e => e.WindDirection)
    .HasConversion<string>()
    .HasMaxLength(10);

// TimeSpan як рядок "HH:mm"
entity.Property(e => e.Sunrise)
    .HasConversion(
        ts => ts.ToString(@"hh\:mm"),
        s => TimeSpan.Parse(s)
    );

// Зв'язок 1:1
entity.HasOne(e => e.WindData)
    .WithOne(w => w.WeatherRecord)
    .HasForeignKey<WeatherRecord>(e => e.WindDataId)
    .OnDelete(DeleteBehavior.SetNull);
```

---

## 4. Зчитування даних з CSV

Клас `WeatherCsvParser` реалізує зчитування CSV-файлу датасету Global Weather Repository (142 678 записів).

Особливості реалізації:
- заголовки зчитуються динамічно — порядок колонок у файлі не важливий
- реалізована коректна обробка полів у лапках з комами всередині
- при помилці в окремому рядку він пропускається з виводом попередження, програма не зупиняється
- `DateTime` парситься з явним `DateTimeKind.Unspecified` для сумісності з PostgreSQL типом `timestamp without time zone`

Збереження відбувається батчами по 500 записів:

```csharp
for (int i = 0; i < rows.Count; i += batchSize)
{
    var batch = rows.Skip(i).Take(batchSize).Select(t => t.Record).ToList();
    await _repo.AddRangeAsync(batch);
    await _repo.SaveChangesAsync();
    Console.WriteLine($"[Import] -> Збережено {total}/{rows.Count}");
}
```

---

## 5. Рефакторинг-міграція

### Початковий стан

До рефакторингу всі дані зберігались в одній таблиці `weather_records`, включаючи поля вітру (`wind_kph`, `wind_mph`, `wind_degree`, `wind_direction`, `gust_kph`, `gust_mph`, `sunrise`, `sunset`).

### Після рефакторингу

Дані вітру винесені в окрему таблицю `wind_data`. Зв'язок між таблицями — один до одного через зовнішній ключ `WindDataId`.

```
weather_records          wind_data
───────────────          ─────────────────────
Id (PK)                  Id (PK)
Country                  WindKph
Location                 WindMph
Condition                WindDegree
LastUpdated              WindDirection
TempC                    GustKph
FeelsLikeC               GustMph
Humidity                 Sunrise
WindDataId (FK) ──────►  Sunset
                         IsGoodToGoOutside
```

Міграція виконана інструментом **EF Core Migrations**. Дані при міграції не втрачаються — переносяться через SQL `INSERT INTO ... SELECT` перед видаленням старих колонок.

---

## 6. Колонка IsGoodToGoOutside

В таблиці `wind_data` додана булева колонка `IsGoodToGoOutside` — відповідь на питання "чи варто виходити на вулицю".

### Формула

```csharp
public bool CalculateIsGoodToGoOutside()
{
    return WindKph < 30.0 && GustKph < 50.0;
}
```

Логіка: якщо швидкість вітру менше 30 км/год **і** пориви менше 50 км/год — виходити можна (повертає `true`), інакше — не варто (`false`).

### Дефолтне значення

Колонка є `nullable bool` — за замовчуванням `NULL`. Це свідоме рішення: поки дані не обчислені або не завантажені, відповідь невизначена. Встановлювати `false` за замовчуванням було б семантично неправильно.

### Де заповнюється

Функція заповнення колонки розподілена між шарами:
- **Domain** (`WindData.CalculateIsGoodToGoOutside`) — де живе сама формула
- **Infrastructure** (`WeatherCsvParser`) — виклик при парсингу кожного рядка CSV
- **Application** (`WeatherService.RecalculateGoOutsideAsync`) — масовий перерахунок для існуючих записів через пункт меню

---

## 7. Пошук погодних даних

Користувач може задати країну, дату та опційно місто і отримати всю наявну інформацію про погоду.

```csharp
public async Task<List<WeatherRecord>> GetByCountryAndDateAsync(
    string country, DateTime date, string? location = null)
{
    var query = _db.WeatherRecords
        .Include(r => r.WindData)
        .Where(r => r.Country.ToLower() == country.ToLower()
                 && r.LastUpdated.Date == date.Date);

    if (!string.IsNullOrWhiteSpace(location))
        query = query.Where(r => r.Location.ToLower().Contains(location.ToLower()));

    return await query.ToListAsync();
}
```

`Include(r => r.WindData)` — eager loading, EF Core підтягує пов'язані дані вітру одним SQL запитом через JOIN.

Приклад виводу в консолі:

```
┌─ Kyiv, Ukraine [2024-01-15 12:00]
│  Стан: Partly cloudy
│  Температура: -3.2°C (відчувається -7.1°C), Вологість: 78%
│  Вітер: 18.4 км/год (11.4 mph), NW (315°)
│  Пориви: 28.1 км/год (17.5 mph)
│  Схід: 7:52 AM, Захід: 4:38 PM
└  Виходити на вулицю: ✅ ТАК
```

---

## 8. Міграції EF Core

Для управління схемою бази використовується **EF Core Migrations** — вбудований інструмент, аналогічний Flyway або Liquibase.

### Команди

```bash
# Згенерувати міграцію
dotnet ef migrations add InitialCreate \
  --project lab3.Infrastructure \
  --startup-project lab3.Console

# Застосувати до бази
dotnet ef database update \
  --project lab3.Infrastructure \
  --startup-project lab3.Console
```

### Принцип роботи

EF Core зберігає в базі таблицю `__EFMigrationsHistory` зі списком вже застосованих міграцій. При кожному запуску `database update` перевіряється які міграції ще не застосовані і накочуються лише вони. Це дозволяє безпечно запускати команду повторно.

Кожна міграція містить два методи: `Up` (застосувати) та `Down` (відкотити). Завдяки цьому можна повернутись до будь-якого попереднього стану схеми.

Для роботи EF CLI без запуску програми реалізована фабрика `WeatherDbContextFactory`:

```csharp
public class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
{
    public WeatherDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=weather_db;...");
        return new WeatherDbContext(optionsBuilder.Options);
    }
}
```

---

## 9. Відповіді на питання до роботи

**Чи можна завантажувати всю базу, а описувати в ORM моделі тільки деякі колонки?**

Так. EF Core дозволяє не включати всі колонки таблиці в ORM-модель. Поля, не описані в `OnModelCreating`, ігноруються при читанні та записі, але залишаються в базі даних. Це корисно коли таблиця має багато колонок, але застосунок працює лише з частиною з них.

**Чи є дефолтне значення при створенні у колонці "Чи варто виходити на вулицю"?**

Дефолтного значення немає — колонка `nullable` (`bool?`), тобто за замовчуванням `NULL`. Це правильно: до моменту обчислення відповідь невизначена. Встановлення `false` за замовчуванням було б некоректним, бо означало б "не варто виходити" навіть для записів де дані просто ще не завантажені.

**У скільки етапів виконано третє завдання?**

В 2 SQL-етапи в рамках однієї міграції:
1. `CREATE TABLE wind_data` + `INSERT INTO wind_data SELECT ... FROM weather_records`
2. `ADD COLUMN WindDataId` + `UPDATE weather_records SET WindDataId = ...` + `DROP COLUMN` для старих полів вітру

**На яку частину застосунку покладено функцію заповнення IsGoodToGoOutside?**

На кілька шарів відповідно до їх відповідальності: сама формула — в **Domain** (`WindData.CalculateIsGoodToGoOutside`), виклик при імпорті — в **Infrastructure** (`WeatherCsvParser`), масовий перерахунок — в **Application** (`WeatherService.RecalculateGoOutsideAsync`).

**Чи легко було накочувати міграції, перейти з однієї бази на іншу?**

Накочування міграцій через EF Core — зручне і автоматичне. Основні труднощі:
- Npgsql за замовчуванням вимагає `DateTime` у форматі UTC, що призвело до помилки при першому імпорті. Вирішено через тип `timestamp without time zone` та явне `DateTimeKind.Unspecified`
- При зміні схеми (`HasColumnType`) потрібно видаляти стару міграцію і генерувати нову, інакше EF Core кидає `PendingModelChangesWarning`

---

## 10. Висновок

В ході виконання лабораторної роботи було реалізовано:

- застосунок на C# з архітектурою Layered Architecture (Domain, Infrastructure, Application, Console)
- ORM-модель з усіма обов'язковими типами даних: текстова, ціле, дробне, enum, дата, час, булеан
- зчитування CSV-файлу датасету Global Weather Repository (142 678 записів) з пакетним збереженням
- рефакторинг-міграцію схеми бази: винесення даних вітру з головної таблиці в окрему `wind_data` без втрати даних
- булеву колонку `IsGoodToGoOutside` з формулою на основі швидкості вітру та поривів
- консольний інтерфейс для пошуку погодних даних за країною, датою та містом
- управління схемою бази через EF Core Migrations
