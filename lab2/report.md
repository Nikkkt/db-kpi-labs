# Лабораторна робота №2
## Реалізація каунтера з використанням PostgreSQL

**Виконав:** _(ПІБ)_  
**Група:** _(група)_  
**Дата:** _(дата)_

---

## Мета роботи

Реалізувати оновлення значення каунтера в СКБД PostgreSQL декількома способами та оцінити час виконання і коректність результату для кожного з варіантів в умовах конкурентного доступу.

---

## Структура таблиці

```sql
CREATE TABLE IF NOT EXISTS user_counter (
    user_id INTEGER PRIMARY KEY,
    counter INTEGER NOT NULL DEFAULT 0,
    version INTEGER NOT NULL DEFAULT 0
);
```

| Поле | Тип | Опис |
|---|---|---|
| `user_id` | INTEGER | Первинний ключ, ідентифікатор користувача |
| `counter` | INTEGER | Значення каунтера |
| `version` | INTEGER | Допоміжне поле для optimistic concurrency |

---

## Умови тестування

- **Мова реалізації:** C# (.NET 8), бібліотека Npgsql
- **Кількість потоків:** 10
- **Ітерацій на потік:** 10 000
- **Очікуване фінальне значення:** 100 000
- Кожен потік має власне підключення до БД
- ORM не використовується, всі запити — сирий SQL

---

## Варіант 1 — Lost Update

### Опис

Кожен потік читає поточне значення каунтера в пам'ять, збільшує його та записує назад. Між SELECT і UPDATE немає синхронізації, тому потоки перезаписують зміни один одного.

### Код

```csharp
for (int i = 0; i < iterations; i++)
{
    // Читаємо значення в пам'ять
    int counter;
    await using (var selectCmd = conn.CreateCommand())
    {
        selectCmd.CommandText =
            "SELECT counter FROM user_counter WHERE user_id = 1";
        counter = (int)(await selectCmd.ExecuteScalarAsync())!;
    }

    // Збільшуємо в додатку
    counter++;

    // Записуємо назад — без будь-якої синхронізації
    await using (var updateCmd = conn.CreateCommand())
    {
        updateCmd.CommandText =
            "UPDATE user_counter SET counter = @counter WHERE user_id = @id";
        updateCmd.Parameters.AddWithValue("counter", counter);
        updateCmd.Parameters.AddWithValue("id", 1);
        await updateCmd.ExecuteNonQueryAsync();
    }
    // autocommit — кожен запит є окремою транзакцією
}
```

### Результат

| Параметр | Значення |
|---|---|
| Час виконання | ~88.8 с |
| Фінальне значення каунтера | ~12 000–18 000 |
| Коректний результат | ✗ |

> Фінальне значення суттєво менше за 100 000 через race condition. Значення непередбачуване і змінюється від запуску до запуску.

---

## Варіант 2 — In-place Update

### Опис

Збільшення каунтера виконується атомарно безпосередньо в PostgreSQL одним SQL-запитом. Читання і запис відбуваються як єдина нерозривна операція на стороні БД — race condition неможливий.

### Код

```csharp
for (int i = 0; i < iterations; i++)
{
    await using var cmd = conn.CreateCommand();
    // Атомарний UPDATE — читання і запис в одній операції
    cmd.CommandText =
        "UPDATE user_counter SET counter = counter + 1 WHERE user_id = @id";
    cmd.Parameters.AddWithValue("id", 1);
    await cmd.ExecuteNonQueryAsync();
    // autocommit
}
```

### Результат

| Параметр | Значення |
|---|---|
| Час виконання | ~68.4 с |
| Фінальне значення каунтера | 100 000 |
| Коректний результат | ✓ |

> Найшвидший коректний варіант. Немає явних блокувань, немає round-trip між читанням і записом.

---

## Варіант 3 — Row-level Locking

### Опис

`SELECT ... FOR UPDATE` блокує рядок до кінця транзакції. Інші потоки очікують на цьому ж SELECT, поки поточний потік не виконає COMMIT. Кожен потік має окреме підключення — обов'язкова умова коректної роботи блокування.

### Код

```csharp
for (int i = 0; i < iterations; i++)
{
    // Явна транзакція — обов'язково для FOR UPDATE
    await using var tx = await conn.BeginTransactionAsync();

    int counter;
    await using (var selectCmd = conn.CreateCommand())
    {
        selectCmd.Transaction = tx;
        // FOR UPDATE блокує рядок до COMMIT
        selectCmd.CommandText =
            "SELECT counter FROM user_counter WHERE user_id = 1 FOR UPDATE";
        counter = (int)(await selectCmd.ExecuteScalarAsync())!;
    }

    counter++;

    await using (var updateCmd = conn.CreateCommand())
    {
        updateCmd.Transaction = tx;
        updateCmd.CommandText =
            "UPDATE user_counter SET counter = @counter WHERE user_id = @id";
        updateCmd.Parameters.AddWithValue("counter", counter);
        updateCmd.Parameters.AddWithValue("id", 1);
        await updateCmd.ExecuteNonQueryAsync();
    }

    // Блокування знімається — наступний потік може продовжити
    await tx.CommitAsync();
}
```

### Результат

| Параметр | Значення |
|---|---|
| Час виконання | ~174.5 с |
| Фінальне значення каунтера | 100 000 |
| Коректний результат | ✓ |

> Результат коректний, але час найбільший серед усіх варіантів — потоки виконуються фактично послідовно через явні блокування.

---

## Варіант 4 — Optimistic Concurrency Control

### Опис

Потік читає `counter` і `version`, збільшує counter і намагається записати з умовою `AND version = @oldVersion`. Якщо за час між читанням і записом інший потік встиг оновити рядок — `version` змінилась, UPDATE не зачепить жодного рядка (`affected = 0`), і поточний потік повторює спробу з нового читання.

### Код

```csharp
for (int i = 0; i < iterations; i++)
{
    bool success = false;
    while (!success)
    {
        // Читаємо counter і version
        int counter, version;
        await using (var selectCmd = conn.CreateCommand())
        {
            selectCmd.CommandText =
                "SELECT counter, version FROM user_counter WHERE user_id = 1";
            await using var reader = await selectCmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            counter = reader.GetInt32(0);
            version = reader.GetInt32(1);
        }

        counter++;

        await using (var updateCmd = conn.CreateCommand())
        {
            // Записуємо лише якщо version не змінилась
            updateCmd.CommandText = @"
                UPDATE user_counter
                SET counter = @counter, version = @newVersion
                WHERE user_id = @id AND version = @oldVersion";
            updateCmd.Parameters.AddWithValue("counter", counter);
            updateCmd.Parameters.AddWithValue("newVersion", version + 1);
            updateCmd.Parameters.AddWithValue("id", 1);
            updateCmd.Parameters.AddWithValue("oldVersion", version);

            int affected = await updateCmd.ExecuteNonQueryAsync();
            if (affected > 0)
                success = true;  // Успіх — виходимо з retry loop
            else
                localRetries++;  // Конфлікт — повторюємо
        }
    }
}
```

### Результат

| Параметр | Значення |
|---|---|
| Час виконання | ~292.8 с |
| Фінальне значення каунтера | 100 000 |
| Кількість повторних спроб (retries) | ~500 000 |
| Коректний результат | ✓ |

> Результат коректний, але кількість повторних спроб дуже велика при 10 конкурентних потоках. Підхід ефективний лише при низькій конкуренції.

---

## Порівняльна таблиця результатів

| Варіант | Час виконання | Фінальне значення | Коректний |
|---|---|---|---|
| 1. Lost Update | ~88.8 с | ~13 000 | ✗ |
| 2. In-place Update | ~68.4 с | 100 000 | ✓ |
| 3. Row-level Locking | ~174.5 с | 100 000 | ✓ |
| 4. Optimistic Concurrency | ~292.8 с | 100 000 | ✓ |

> Значення часу є приблизними і залежать від конфігурації системи та навантаження на БД.

---

## Висновки

1. **Lost Update** демонструє класичну проблему race condition при відсутності будь-якої синхронізації. Фінальне значення непередбачуване і завжди менше за очікуване. Цей підхід неприйнятний у реальних системах.

2. **In-place Update** — найпростіший і найефективніший коректний варіант. Атомарність забезпечується самою СУБД без жодних додаткових механізмів синхронізації.

3. **Row-level Locking** гарантує коректність через явні блокування, але при великій кількості потоків перетворюється на послідовне виконання, що суттєво збільшує час.

4. **Optimistic Concurrency** підходить для сценаріїв з низькою конкуренцією. При 10 одночасних потоках кількість повторних спроб у рази перевищує кількість успішних операцій, що робить цей підхід неефективним у даному сценарії.

---

## Використані технології

- **Мова:** C# (.NET 8)
- **СУБД:** PostgreSQL
- **Бібліотека:** Npgsql 8.0.3
- **Паралелізм:** `Task.Run`, `async/await`
