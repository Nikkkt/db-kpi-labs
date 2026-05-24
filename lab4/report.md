# Лабораторна робота №4
## Робота з базовими функціями документо-орієнтованої БД на прикладі MongoDB

**Виконав:** _Терпіловський Нікіта_  
**Мова реалізації:** C# / .NET 10  
**Драйвер:** MongoDB.Driver 3.4.0  
**База даних:** `shop`  

---

## Частина 1 — Товари (`items`)

---

### Завдання 1. Створення товарів з різним набором властивостей

Колекція `items`. Кожен документ має різний набір полів залежно від категорії:
- **Phone** — `storage`, `color` / `foldable`
- **TV** — `screen_size`, `resolution` / `smart`
- **Smart Watch** — `battery_hours`, `water_resist` / `health_sensor`
- **Laptop** — `ram_gb`, `cpu`

**Команда:**
```csharp
await col.InsertManyAsync(products);
```

**Результат:**
```
1. Товари додано:
   Phone        | iPhone 15 Pro              | $1199
   Phone        | Galaxy S24                 | $899
   TV           | OLED55C3                   | $1500
   TV           | QN85QN90C                  | $1800
   Smart Watch  | Apple Watch Series 9       | $399
   Smart Watch  | Galaxy Watch 6             | $299
   Laptop       | MacBook Pro 14             | $1999
   Laptop       | XPS 15                     | $1699
```

---

### Завдання 2. Вивести всі товари

**Команда:**
```csharp
var all = await col.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
```

**Результат:**
```json
{ "_id": { "$oid": "6a1371548d3095ff18fa90d0" }, "category": "Phone", "model": "iPhone 15 Pro", "producer": "Apple", "price": 1199.0, "storage": "256GB", "color": "Titanium" }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d1" }, "category": "Phone", "model": "Galaxy S24", "producer": "Samsung", "price": 899.0, "storage": "128GB", "foldable": false }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d2" }, "category": "TV", "model": "OLED55C3", "producer": "LG", "price": 1500.0, "screen_size": 55, "resolution": "4K" }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d3" }, "category": "TV", "model": "QN85QN90C", "producer": "Samsung", "price": 1800.0, "screen_size": 85, "smart": true }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d4" }, "category": "Smart Watch", "model": "Apple Watch Series 9", "producer": "Apple", "price": 399.0, "battery_hours": 18, "water_resist": "WR50" }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d5" }, "category": "Smart Watch", "model": "Galaxy Watch 6", "producer": "Samsung", "price": 299.0, "battery_hours": 40, "health_sensor": true }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d6" }, "category": "Laptop", "model": "MacBook Pro 14", "producer": "Apple", "price": 1999.0, "ram_gb": 16, "cpu": "M3 Pro" }
{ "_id": { "$oid": "6a1371548d3095ff18fa90d7" }, "category": "Laptop", "model": "XPS 15", "producer": "Dell", "price": 1699.0, "ram_gb": 32, "cpu": "Intel Core i9" }
```

---

### Завдання 3. Кількість товарів у певній категорії

**Команда:**
```csharp
long cnt = await col.CountDocumentsAsync(
    Builders<BsonDocument>.Filter.Eq("category", "Phone"));
```

**Результат:**
```
3. Кількість товарів у категорії 'Phone': 2
```

---

### Завдання 4. Кількість різних категорій

Використано агрегаційний pipeline: перший `$group` групує по полю `category`, другий `$group` рахує кількість отриманих груп.

**Команда:**
```csharp
BsonDocument[] pipeline =
[
    new("$group", new BsonDocument("_id", "$category")),
    new("$group", new BsonDocument {
        ["_id"]   = BsonNull.Value,
        ["count"] = new BsonDocument("$sum", 1)
    }),
];
var catRes = await col.Aggregate<BsonDocument>(pipeline).ToListAsync();
```

**Результат:**
```
4. Кількість різних категорій: 4
```

---

### Завдання 5. Список всіх виробників без повторів

**Команда:**
```csharp
var dist = await col.DistinctAsync<string>("producer",
    Builders<BsonDocument>.Filter.Empty);
```

**Результат:**
```
5. Виробники без повторів: Apple, Dell, LG, Samsung
```

---

### Завдання 6а. Вибірка за критеріями — `$and`

Товари категорії Phone з ціною від $500 до $1000.

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.And(
    Builders<BsonDocument>.Filter.Eq("category", "Phone"),
    Builders<BsonDocument>.Filter.Gte("price", 500),
    Builders<BsonDocument>.Filter.Lte("price", 1000));
```

**Результат:**
```
6a. $and (Phone, $500–$1000):
   Galaxy S24 — $899
```

---

### Завдання 6б. Вибірка за критеріями — `$or`

Товари з моделлю «iPhone 15 Pro» або «Galaxy S24».

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.Or(
    Builders<BsonDocument>.Filter.Eq("model", "iPhone 15 Pro"),
    Builders<BsonDocument>.Filter.Eq("model", "Galaxy S24"));
```

**Результат:**
```
6b. $or (iPhone 15 Pro або Galaxy S24):
   iPhone 15 Pro
   Galaxy S24
```

---

### Завдання 6в. Вибірка за критеріями — `$in`

Товари виробників Apple або Dell.

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.In("producer", ["Apple", "Dell"]);
```

**Результат:**
```
6c. $in (Apple або Dell):
   iPhone 15 Pro (Apple)
   Apple Watch Series 9 (Apple)
   MacBook Pro 14 (Apple)
   XPS 15 (Dell)
```

---

### Завдання 7. Оновлення товарів — зміна ціни та додавання нового поля

Для всіх товарів категорії TV встановлено нову ціну та додано поле `warranty_years`.

**Команда:**
```csharp
await col.UpdateManyAsync(
    Builders<BsonDocument>.Filter.Eq("category", "TV"),
    Builders<BsonDocument>.Update
        .Set("price", 1399.0)
        .Set("warranty_years", 3));
```

**Результат:**
```
7. Оновлено TV (price=1399, +warranty_years=3): 2 шт.
```

---

### Завдання 8. Знайти товари де є певне поле (`$exists`)

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.Exists("battery_hours", true);
```

**Результат:**
```
8. Товари з полем 'battery_hours':
   Apple Watch Series 9 — 18h
   Galaxy Watch 6 — 40h
```

---

### Завдання 9. Збільшити вартість знайдених товарів на певну суму (`$inc`)

**Команда:**
```csharp
await col.UpdateManyAsync(existsFilter,
    Builders<BsonDocument>.Update.Inc("price", 50.0));
```

**Результат:**
```
9. Ціна товарів з 'battery_hours' +$50 (2 шт.):
   Apple Watch Series 9 → $449
   Galaxy Watch 6 → $349
```

---

## Частина 2 — Замовлення (`orders`)

Структура замовлення:
- **`customer`** — вбудований документ (**embed**)
- **`items_id`** — масив ObjectId, посилання на колекцію `items` (**reference**)

---

### Завдання 10. Вивести всі замовлення

**Команда:**
```csharp
var allO = await orders.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
```

**Результат:**
```
10. Всі замовлення:
   #1001 | Олег Ковальчук | $1598
   #1002 | Марія Петренко | $2899
   #1003 | Олег Ковальчук | $2398
```

---

### Завдання 11. Замовлення з вартістю більше певного значення

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.Gt("total_sum", 2000);
```

**Результат:**
```
11. Замовлення > $2000:
   #1002 | $2899
   #1003 | $2398
```

---

### Завдання 12. Знайти замовлення одного замовника

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.Eq("customer.name", "Олег");
```

**Результат:**
```
12. Замовлення 'Олег':
   #1001 | 2024-03-09
   #1003 | 2024-05-19
```

---

### Завдання 13. Знайти замовлення з певним товаром по ObjectId

**Команда:**
```csharp
var filter = Builders<BsonDocument>.Filter.AnyEq("items_id", idPhone1);
```

**Результат:**
```
13. Замовлення з 'iPhone 15 Pro':
   #1001
   #1002
```

---

### Завдання 14. Додати товар до знайдених замовлень та збільшити вартість

**Команда:**
```csharp
await orders.UpdateManyAsync(itemFilter,
    Builders<BsonDocument>.Update
        .AddToSet("items_id", idWatch1)
        .Inc("total_sum", 449.0));
```

**Результат:**
```
14. +Apple Watch +$449 до замовлень з iPhone: 2 шт.
```

---

### Завдання 15. Кількість товарів у певному замовленні

**Команда:**
```csharp
var o1001 = await orders.Find(
    Builders<BsonDocument>.Filter.Eq("order_number", 1001)).FirstAsync();
int count = o1001["items_id"].AsBsonArray.Count;
```

**Результат:**
```
15. Товарів у замовленні #1001: 3
```

---

### Завдання 16. Проекція — тільки customer та номер картки

**Команда:**
```csharp
var proj = Builders<BsonDocument>.Projection
    .Include("customer")
    .Include("payment.cardId")
    .Exclude("_id");
var result = await orders.Find(bigFilter).Project(proj).ToListAsync();
```

**Результат:**
```json
{ "customer": { "name": "Олег", "surname": "Ковальчук", "phones": [380501234567, 380671234567], "address": "вул. Хрещатик 1, Київ, UA" }, "payment": { "cardId": 4111111111111111 } }
{ "customer": { "name": "Марія", "surname": "Петренко", "phones": [380931234567], "address": "пр. Перемоги 37, Київ, UA" }, "payment": { "cardId": 5500005555555559 } }
{ "customer": { "name": "Олег", "surname": "Ковальчук", "phones": [380501234567], "address": "вул. Хрещатик 1, Київ, UA" }, "payment": { "cardId": 4111111111111111 } }
```

---

### Завдання 17. Видалити товар із замовлень за певний період дат (`$pull`)

**Команда:**
```csharp
var dateFilter = Builders<BsonDocument>.Filter.And(
    Builders<BsonDocument>.Filter.Gte("date", new BsonDateTime(new DateTime(2024, 1,  1))),
    Builders<BsonDocument>.Filter.Lte("date", new BsonDateTime(new DateTime(2024, 4, 30))));

await orders.UpdateManyAsync(dateFilter,
    Builders<BsonDocument>.Update.Pull("items_id", idPhone2));
```

**Результат:**
```
17. Видалено 'Galaxy S24' із замовлень 01.01–30.04.2024: 1 шт.
```

---

### Завдання 18. Перейменувати прізвище замовника у всіх замовленнях

**Команда:**
```csharp
await orders.UpdateManyAsync(
    Builders<BsonDocument>.Filter.Eq("customer.name", "Олег"),
    Builders<BsonDocument>.Update.Set("customer.surname", "Ковалів"));
```

**Результат:**
```
18. 'Ковальчук' → 'Ковалів' для Олега: 2 замовлень.
```

---

### Завдання 19. JOIN між `orders` та `items` (аналог JOIN між таблицями)

Беремо масив `items_id` із замовлення та робимо `Find` по колекції `items` з фільтром `$in` — підставляємо назви і ціни товарів замість ObjectId.

**Команда:**
```csharp
var ids = order["items_id"].AsBsonArray.Select(v => v.AsObjectId).ToList();
var joinItems = await items.Find(
    Builders<BsonDocument>.Filter.In("_id", ids)).ToListAsync();
```

**Результат:**
```
19. JOIN замовлень Марії (items_id → model + price):
   #1002 | Марія Петренко
      iPhone 15 Pro                | $1199
      OLED55C3                     | $1399
      Apple Watch Series 9         | $449
```

---

## Частина 3 — Capped Collection (`reviews`)

Capped collection — колекція з фіксованим розміром. При досягненні ліміту найстаріші документи автоматично видаляються. Використовується для зберігання останніх N записів без ручного очищення.

---

### Завдання 20. Створення та перевірка Capped Collection

Ліміт: **5 документів**. Вставляємо 7 відгуків — перші 2 автоматично видаляються.

**Команда:**
```csharp
await db.CreateCollectionAsync("reviews", new CreateCollectionOptions
{
    Capped       = true,
    MaxDocuments = 5,
    MaxSize      = 10_240
});

// Вставка 7 відгуків...
await reviews.InsertOneAsync(new BsonDocument { ... });
```

**Результат:**
```
20. Capped collection 'reviews' (вставлено 7, збережено 5 — ліміт 5):
   #3  Петро Гриценко     5  Відмінна якість товарів!
   #4  Наталя Коваль      3  Середнє обслуговування.
   #5  Артем Бойко        5  Рекомендую всім! Топ магазин.
   #6  Лариса Сорока      4  Гарний вибір, повернусь ще.
   #7  Дмитро Хмара       2  Довго чекав, але товар прийшов.
```

Відгуки №1 (Іван Мельник) та №2 (Оксана Білик) були автоматично видалені при вставці 6-го та 7-го документів. Це підтверджує коректну роботу Capped Collection.

---

## Висновок

В ході лабораторної роботи було реалізовано:

- моделювання інтернет-магазину в MongoDB з колекціями `items`, `orders`, `reviews`
- вставку документів з різним набором полів (гнучкість документо-орієнтованої моделі)
- базові CRUD-операції та запити з операторами `$and`, `$or`, `$in`, `$exists`, `$set`, `$inc`, `$pull`, `$addToSet`
- агрегаційний pipeline для підрахунку унікальних категорій
- два підходи до моделювання зв'язків: **embed** (замовник) та **reference** (товари)
- аналог JOIN між колекціями через `$in` по масиву ObjectId
- Capped Collection з автоматичним витісненням старих записів
