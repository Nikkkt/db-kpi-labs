using lab1;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Лабораторна робота №1");
Console.WriteLine();

DemoHashFunction();

var tree = BuildPhoneBook();

tree.PrintTree();

DemoSearch(tree);

DemoRangeSearch(tree);

DemoDelete(tree);

void DemoHashFunction()
{
    Console.WriteLine("1. ХЕШ-ФУНКЦІЯ");
    Console.WriteLine("Формат хешу: [PPP][MMMMMMM][LL]");
    Console.WriteLine("PPP – перші 3 літери (група 1–9), MM – rolling hash");
    Console.WriteLine("LL – довжина слова (01–99)");
 
    string[] names =
    {
        "Андрій", "Анна", "Борис", "Зайченко", "Зайчатко",
        "Михайло", "Микола", "Тетяна", "Тетянка",
        "Куриця", "Курчак",
        "Ал", "Александр"
    };
 
    foreach (var n in names) Console.WriteLine("  " + NameHashFunction.Explain(n));
}

BPlusTree BuildPhoneBook()
{
    Console.WriteLine("\n2. ВСТАВКА В ДЕРЕВО");
    Console.WriteLine("Order=4 (max 7 ключів у вузлі, мін 3 для некореневих)");

    var people = new[]
    {
        new Person("Андрій Захарченко",    "+380501234567", "вул. Рибна 1"),
        new Person("Анна Борченко",        "+380502345678", "вул. Садова 5"),
        new Person("Борис Петренко",       "+380503456789", "вул. Дніпровська 10"),
        new Person("Василь Коваль",        "+380504567890", "вул. Центральна 3"),
        new Person("Галина Мороз",         "+380505678901", "вул. Лісова 7"),
        new Person("Дмитро Шевченко",      "+380506789012", "вул. Соборна 15"),
        new Person("Євген Іваненко",       "+380507890123", "вул. Приморська 2"),
        new Person("Зоя Кравченко",        "+380508901234", "вул. Весняна 8"),
        new Person("Іван Франко",          "+380509012345", "вул. Франківська 9"),
        new Person("Катерина Бондар",      "+380501122334", "вул. Злагоди 4"),
        new Person("Леся Тищенко",         "+380502233445", "вул. Мирна 6"),
        new Person("Михайло Грищенко",     "+380503344556", "вул. Херсонська 11"),
        new Person("Наталія Поліщук",      "+380504455667", "вул. Перемоги 13"),
        new Person("Олег Сидоренко",       "+380505566778", "вул. Зоряна 21"),
        new Person("Петро Власенко",       "+380506677889", "вул. Чорноморська 17"),
        new Person("Роман Савченко",       "+380507788990", "вул. Балтійська 3"),
        new Person("Світлана Кузьменко",   "+380508899001", "вул. Набережна 19"),
        new Person("Тарас Костенко",       "+380509900112", "вул. Козацька 22"),
        new Person("Уляна Демченко",       "+380501010203", "вул. Квіткова 14"),
        new Person("Федір Гонта",          "+380502020304", "вул. Гетьманська 5"),
    };

    var tree = new BPlusTree();
    foreach (var p in people)
    {
        Console.WriteLine($"  + Вставка: {p.Name,-25} хеш={NameHashFunction.Compute(p.Name)}");
        tree.Insert(p);
    }
    Console.WriteLine();
    return tree;
}

void DemoSearch(BPlusTree tree)
{
    Console.WriteLine("3. ТОЧНИЙ ПОШУК");
 
    string[] searchNames = { "Іван Франко", "Тарас Костенко", "Невідомий" };
 
    foreach (var name in searchNames)
    {
        var found = tree.Search(name);
        if (found.Count == 0) Console.WriteLine($"  \"{name}\" - не знайдено");
        else foreach (var p in found) Console.WriteLine($"  \"{name}\" - {p.Phone}, {p.Address}");
    }
    Console.WriteLine();
}

void DemoRangeSearch(BPlusTree tree)
{
    Console.WriteLine("4. ДІАПАЗОННИЙ ПОШУК");
 
    string pivot = "Михайло Грищенко";
    Console.WriteLine($"  Пошук усіх, хто йде ПІСЛЯ або РАЗОМ з \"{pivot}\":");
    var ge = tree.SearchGreaterOrEqual(pivot);
    foreach (var p in ge) Console.WriteLine($"    ≥ {p.Name,-25} {p.Phone}");
 
    Console.WriteLine();
    Console.WriteLine($"  Пошук усіх, хто йде ДО \"{pivot}\":");
    var lt = tree.SearchLess(pivot);
    foreach (var p in lt) Console.WriteLine($"    < {p.Name,-25} {p.Phone}");
 
    Console.WriteLine();
}

void DemoDelete(BPlusTree tree)
{
    Console.WriteLine("5. ВИДАЛЕННЯ");
 
    string name = "Борис Петренко";
    Console.WriteLine($"  Видаляємо: \"{name}\"");
    bool ok = tree.Delete(name);
    Console.WriteLine($"  Результат: {(ok ? "успішно видалено" : "не знайдено")}");
 
    Console.WriteLine($"\n  Перевірка пошуком після видалення:");
    var check = tree.Search(name);
    Console.WriteLine(check.Count == 0 ? $"  \"{name}\" — відсутній" : "  Ще є записи");
 
    tree.PrintTree();
}

