using MongoDB.Bson;
using MongoDB.Driver;

var client = new MongoClient("mongodb://localhost:27017");
var db     = client.GetDatabase("shop");

await Part1_Items(db);
await Part2_Orders(db);
await Part3_CappedCollection(db);

static async Task Part1_Items(IMongoDatabase db)
{
    var col = db.GetCollection<BsonDocument>("items");
    await col.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

    // 1. Додати товари
    List<BsonDocument> products =
    [
        new() { ["category"]="Phone",       ["model"]="iPhone 15 Pro",         ["producer"]="Apple",   ["price"]=1199.0, ["storage"]="256GB",  ["color"]="Titanium"    },
        new() { ["category"]="Phone",       ["model"]="Galaxy S24",             ["producer"]="Samsung", ["price"]=899.0,  ["storage"]="128GB",  ["foldable"]=false      },
        new() { ["category"]="TV",          ["model"]="OLED55C3",               ["producer"]="LG",      ["price"]=1500.0, ["screen_size"]=55,   ["resolution"]="4K"     },
        new() { ["category"]="TV",          ["model"]="QN85QN90C",              ["producer"]="Samsung", ["price"]=1800.0, ["screen_size"]=85,   ["smart"]=true          },
        new() { ["category"]="Smart Watch", ["model"]="Apple Watch Series 9",   ["producer"]="Apple",   ["price"]=399.0,  ["battery_hours"]=18, ["water_resist"]="WR50" },
        new() { ["category"]="Smart Watch", ["model"]="Galaxy Watch 6",         ["producer"]="Samsung", ["price"]=299.0,  ["battery_hours"]=40, ["health_sensor"]=true  },
        new() { ["category"]="Laptop",      ["model"]="MacBook Pro 14",         ["producer"]="Apple",   ["price"]=1999.0, ["ram_gb"]=16,        ["cpu"]="M3 Pro"        },
        new() { ["category"]="Laptop",      ["model"]="XPS 15",                 ["producer"]="Dell",    ["price"]=1699.0, ["ram_gb"]=32,        ["cpu"]="Intel Core i9" },
    ];
    await col.InsertManyAsync(products);
    Print("1. Товари додано:");
    products.ForEach(p => Console.WriteLine($"   {p["category"],-12} | {p["model"],-26} | ${p["price"]}"));

    // 2. Всі товари
    var all = await col.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    Print("\n2. Всі товари:");
    all.ForEach(d => Console.WriteLine("   " + d.ToJson()));

    // 3. Кількість товарів у категорії Phone
    long cnt = await col.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("category", "Phone"));
    Print($"\n3. Кількість товарів у категорії 'Phone': {cnt}");

    // 4. Кількість різних категорій
    BsonDocument[] pipeline =
    [
        new("$group", new BsonDocument("_id", "$category")),
        new("$group", new BsonDocument { ["_id"] = BsonNull.Value, ["count"] = new BsonDocument("$sum", 1) }),
    ];
    var catRes    = await col.Aggregate<BsonDocument>(pipeline).ToListAsync();
    int totalCats = catRes.Count > 0 ? catRes[0]["count"].AsInt32 : 0;
    Print($"\n4. Кількість різних категорій: {totalCats}");

    // 5. Унікальні виробники
    var dist         = await col.DistinctAsync<string>("producer", Builders<BsonDocument>.Filter.Empty);
    var producerList = await dist.ToListAsync();
    Print($"\n5. Виробники без повторів: {string.Join(", ", producerList)}");

    // 6a. and: Phone + ціна 500–1000
    var andF = Builders<BsonDocument>.Filter.And(
        Builders<BsonDocument>.Filter.Eq("category", "Phone"),
        Builders<BsonDocument>.Filter.Gte("price", 500),
        Builders<BsonDocument>.Filter.Lte("price", 1000));
    var andR = await col.Find(andF).ToListAsync();
    Print($"\n6a. $and (Phone, $500–$1000):");
    andR.ForEach(d => Console.WriteLine($"   {d["model"]} — ${d["price"]}"));

    // 6b. or: iPhone 15 Pro або Galaxy S24
    var orF = Builders<BsonDocument>.Filter.Or(
        Builders<BsonDocument>.Filter.Eq("model", "iPhone 15 Pro"),
        Builders<BsonDocument>.Filter.Eq("model", "Galaxy S24"));
    var orR = await col.Find(orF).ToListAsync();
    Print($"\n6b. $or (iPhone 15 Pro або Galaxy S24):");
    orR.ForEach(d => Console.WriteLine($"   {d["model"]}"));

    // 6c. in: виробники Apple або Dell
    var inF = Builders<BsonDocument>.Filter.In("producer", ["Apple", "Dell"]);
    var inR = await col.Find(inF).ToListAsync();
    Print($"\n6c. $in (Apple або Dell):");
    inR.ForEach(d => Console.WriteLine($"   {d["model"]} ({d["producer"]})"));

    // 7. Оновити TV: нова ціна + поле warranty_years
    var tvRes = await col.UpdateManyAsync(
        Builders<BsonDocument>.Filter.Eq("category", "TV"),
        Builders<BsonDocument>.Update.Set("price", 1399.0).Set("warranty_years", 3));
    Print($"\n7. Оновлено TV (price=1399, +warranty_years=3): {tvRes.ModifiedCount} шт.");

    // 8. Товари з полем battery_hours
    var existsF = Builders<BsonDocument>.Filter.Exists("battery_hours", true);
    var withBat = await col.Find(existsF).ToListAsync();
    Print($"\n8. Товари з полем 'battery_hours':");
    withBat.ForEach(d => Console.WriteLine($"   {d["model"]} — {d["battery_hours"]}h"));

    // 9. Збільшити ціну товарів з battery_hours на $50
    var incRes = await col.UpdateManyAsync(existsF, Builders<BsonDocument>.Update.Inc("price", 50.0));
    Print($"\n9. Ціна товарів з 'battery_hours' +$50 ({incRes.ModifiedCount} шт.):");
    (await col.Find(existsF).ToListAsync())
        .ForEach(d => Console.WriteLine($"   {d["model"]} → ${d["price"]}"));
}

static async Task Part2_Orders(IMongoDatabase db)
{
    var items  = db.GetCollection<BsonDocument>("items");
    var orders = db.GetCollection<BsonDocument>("orders");
    await orders.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);

    var all       = await items.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    var idPhone1  = all.First(d => d["model"] == "iPhone 15 Pro")       ["_id"].AsObjectId;
    var idPhone2  = all.First(d => d["model"] == "Galaxy S24")          ["_id"].AsObjectId;
    var idTv1     = all.First(d => d["model"] == "OLED55C3")            ["_id"].AsObjectId;
    var idWatch1  = all.First(d => d["model"] == "Apple Watch Series 9")["_id"].AsObjectId;
    var idLaptop1 = all.First(d => d["model"] == "MacBook Pro 14")      ["_id"].AsObjectId;

    List<BsonDocument> orderDocs =
    [
        new() {
            ["order_number"] = 1001,
            ["date"]         = new BsonDateTime(new DateTime(2024, 3, 10)),
            ["total_sum"]    = 1598.0,
            ["customer"]     = new BsonDocument {
                ["name"]="Олег", ["surname"]="Ковальчук",
                ["phones"]=new BsonArray{380501234567L,380671234567L},
                ["address"]="вул. Хрещатик 1, Київ, UA"
            },
            ["payment"]  = new BsonDocument{["card_owner"]="Oleg Kovalchuk",["cardId"]=4111111111111111L},
            ["items_id"] = new BsonArray{idPhone1, idPhone2}
        },
        new() {
            ["order_number"] = 1002,
            ["date"]         = new BsonDateTime(new DateTime(2024, 4, 5)),
            ["total_sum"]    = 2899.0,
            ["customer"]     = new BsonDocument {
                ["name"]="Марія", ["surname"]="Петренко",
                ["phones"]=new BsonArray{380931234567L},
                ["address"]="пр. Перемоги 37, Київ, UA"
            },
            ["payment"]  = new BsonDocument{["card_owner"]="Mariia Petrenko",["cardId"]=5500005555555559L},
            ["items_id"] = new BsonArray{idTv1, idPhone1}
        },
        new() {
            ["order_number"] = 1003,
            ["date"]         = new BsonDateTime(new DateTime(2024, 5, 20)),
            ["total_sum"]    = 2398.0,
            ["customer"]     = new BsonDocument {
                ["name"]="Олег", ["surname"]="Ковальчук",
                ["phones"]=new BsonArray{380501234567L},
                ["address"]="вул. Хрещатик 1, Київ, UA"
            },
            ["payment"]  = new BsonDocument{["card_owner"]="Oleg Kovalchuk",["cardId"]=4111111111111111L},
            ["items_id"] = new BsonArray{idLaptop1, idWatch1}
        },
    ];
    await orders.InsertManyAsync(orderDocs);

    // 10. Всі замовлення
    var allO = await orders.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    Print("\n10. Всі замовлення:");
    allO.ForEach(o => Console.WriteLine($"   #{o["order_number"]} | {o["customer"]["name"]} {o["customer"]["surname"]} | ${o["total_sum"]}"));

    // 11. Замовлення > $2000
    var bigF = Builders<BsonDocument>.Filter.Gt("total_sum", 2000);
    var bigO = await orders.Find(bigF).ToListAsync();
    Print($"\n11. Замовлення > $2000:");
    bigO.ForEach(o => Console.WriteLine($"   #{o["order_number"]} | ${o["total_sum"]}"));

    // 12. Замовлення одного замовника
    var custO = await orders.Find(Builders<BsonDocument>.Filter.Eq("customer.name", "Олег")).ToListAsync();
    Print($"\n12. Замовлення 'Олег':");
    custO.ForEach(o => Console.WriteLine($"   #{o["order_number"]} | {o["date"].ToUniversalTime():yyyy-MM-dd}"));

    // 13. Замовлення з певним товаром
    var itemF = Builders<BsonDocument>.Filter.AnyEq("items_id", idPhone1);
    var itemO = await orders.Find(itemF).ToListAsync();
    Print($"\n13. Замовлення з 'iPhone 15 Pro':");
    itemO.ForEach(o => Console.WriteLine($"   #{o["order_number"]}"));

    // 14. Додати товар + збільшити суму
    var addRes = await orders.UpdateManyAsync(itemF,
        Builders<BsonDocument>.Update.AddToSet("items_id", idWatch1).Inc("total_sum", 449.0));
    Print($"\n14. +Apple Watch +$449 до замовлень з iPhone: {addRes.ModifiedCount} шт.");

    // 15. Кількість товарів у замовленні #1001
    var o1001 = await orders.Find(Builders<BsonDocument>.Filter.Eq("order_number", 1001)).FirstAsync();
    Print($"\n15. Товарів у замовленні #1001: {o1001["items_id"].AsBsonArray.Count}");

    // 16. Проекція: тільки customer + cardId для замовлень > $2000
    var proj  = Builders<BsonDocument>.Projection.Include("customer").Include("payment.cardId").Exclude("_id");
    var richO = await orders.Find(bigF).Project(proj).ToListAsync();
    Print($"\n16. Customer + cardId (замовлення > $2000):");
    richO.ForEach(o => Console.WriteLine("   " + o.ToJson()));

    // 17. Видалити товар із замовлень за діапазон дат
    var dateF = Builders<BsonDocument>.Filter.And(
        Builders<BsonDocument>.Filter.Gte("date", new BsonDateTime(new DateTime(2024, 1,  1))),
        Builders<BsonDocument>.Filter.Lte("date", new BsonDateTime(new DateTime(2024, 4, 30))));
    var pullRes = await orders.UpdateManyAsync(dateF, Builders<BsonDocument>.Update.Pull("items_id", idPhone2));
    Print($"\n17. Видалено 'Galaxy S24' із замовлень 01.01–30.04.2024: {pullRes.ModifiedCount} шт.");

    // 18. Перейменувати прізвище замовника
    var renRes = await orders.UpdateManyAsync(
        Builders<BsonDocument>.Filter.Eq("customer.name", "Олег"),
        Builders<BsonDocument>.Update.Set("customer.surname", "Ковалів"));
    Print($"\n18. 'Ковальчук' → 'Ковалів' для Олега: {renRes.ModifiedCount} замовлень.");

    // 19. JOIN: замовлення Марії → назви товарів + ціни
    Print("\n19. JOIN замовлень Марії (items_id → model + price):");
    var mariiaOrders = await orders.Find(Builders<BsonDocument>.Filter.Eq("customer.name", "Марія")).ToListAsync();
    foreach (var order in mariiaOrders)
    {
        Console.WriteLine($"   #{order["order_number"]} | {order["customer"]["name"]} {order["customer"]["surname"]}");
        var ids       = order["items_id"].AsBsonArray.Select(v => v.AsObjectId).ToList();
        var joinItems = await items.Find(Builders<BsonDocument>.Filter.In("_id", ids)).ToListAsync();
        joinItems.ForEach(i => Console.WriteLine($"      {i["model"],-28} | ${i["price"]}"));
    }
}

static async Task Part3_CappedCollection(IMongoDatabase db)
{
    await db.DropCollectionAsync("reviews");
    await db.CreateCollectionAsync("reviews", new CreateCollectionOptions
    {
        Capped       = true,
        MaxDocuments = 5,
        MaxSize      = 10_240
    });

    var reviews = db.GetCollection<BsonDocument>("reviews");

    (string author, int rating, string text)[] data =
    [
        ("Іван Мельник",   5, "Чудовий магазин! Швидка доставка."),
        ("Оксана Білик",   4, "Все добре, трохи затримали доставку."),
        ("Петро Гриценко", 5, "Відмінна якість товарів!"),
        ("Наталя Коваль",  3, "Середнє обслуговування."),
        ("Артем Бойко",    5, "Рекомендую всім! Топ магазин."),
        ("Лариса Сорока",  4, "Гарний вибір, повернусь ще."),
        ("Дмитро Хмара",   2, "Довго чекав, але товар прийшов."),
    ];

    int num = 1;
    foreach (var (author, rating, text) in data)
        await reviews.InsertOneAsync(new BsonDocument
        {
            ["review_num"] = num++,
            ["author"]     = author,
            ["rating"]     = rating,
            ["text"]       = text,
            ["date"]       = new BsonDateTime(DateTime.UtcNow),
        });

    var stored = await reviews.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
    Print($"\n20. Capped collection 'reviews' (вставлено 7, збережено {stored.Count} — ліміт 5):");
    stored.ForEach(r => Console.WriteLine($"   #{r["review_num"]} {r["author"],-18} ⭐{r["rating"]} {r["text"]}"));
}

static void Print(string msg)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(msg);
    Console.ResetColor();
}