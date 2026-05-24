using lab2;

namespace Lab2;

class Program
{
    public const string ConnectionString = "Host=localhost;Port=5433;Database=user_lab2;Username=postgres;Password=dblabspass";

    public const int Threads = 10;
    public const int IterationsPerThread = 10_000;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        await DbInit.InitializeAsync(ConnectionString);

        Console.WriteLine("Лабораторна робота №2\n");

        // 1. Lost Update
        await DbInit.ResetCounterAsync(ConnectionString);
        Console.WriteLine("1. Lost Update");
        var lostUpdateTime = await LostUpdate.RunAsync(ConnectionString, Threads, IterationsPerThread);
        int lostVal = await DbInit.GetCounterAsync(ConnectionString);
        Console.WriteLine($"Час: {lostUpdateTime.TotalSeconds:F2}с");
        Console.WriteLine($"Фінальне значення каунтера: {lostVal} (очікується менше {Threads * IterationsPerThread} через race condition)\n");

        // 2. In-place Update
        await DbInit.ResetCounterAsync(ConnectionString);
        Console.WriteLine("2. In-place Update");
        var inPlaceTime = await InPlaceUpdate.RunAsync(ConnectionString, Threads, IterationsPerThread);
        int inPlaceVal = await DbInit.GetCounterAsync(ConnectionString);
        Console.WriteLine($"Час: {inPlaceTime.TotalSeconds:F2}с");
        Console.WriteLine($"Фінальне значення каунтера: {inPlaceVal} (очікується {Threads * IterationsPerThread})\n");

        // 3. Row-level Locking
        await DbInit.ResetCounterAsync(ConnectionString);
        Console.WriteLine("3. Row-level Locking (SELECT FOR UPDATE)");
        var rowLockTime = await RowLevelLocking.RunAsync(ConnectionString, Threads, IterationsPerThread);
        int rowLockVal = await DbInit.GetCounterAsync(ConnectionString);
        Console.WriteLine($"Час: {rowLockTime.TotalSeconds:F2}с");
        Console.WriteLine($"Фінальне значення каунтера: {rowLockVal} (очікується {Threads * IterationsPerThread})\n");

        // 4. Optimistic Concurrency Control
        await DbInit.ResetCounterAsync(ConnectionString);
        Console.WriteLine("4. Optimistic Concurrency Control");
        var optimisticTime = await OptimisticConcurrency.RunAsync(ConnectionString, Threads, IterationsPerThread);
        int optimisticVal = await DbInit.GetCounterAsync(ConnectionString);
        Console.WriteLine($"Час: {optimisticTime.TotalSeconds:F2}с");
        Console.WriteLine($"Фінальне значення каунтера: {optimisticVal} (очікується {Threads * IterationsPerThread})\n");

        Console.WriteLine($"{"Варіант",-35} {"Час (с)",10} {"Результат",12} {"Коректний?",12}");
        Console.WriteLine(new string('-', 72));
        int expected = Threads * IterationsPerThread;
        PrintRow("1. Lost Update",            lostUpdateTime.TotalSeconds,  lostVal,      false);
        PrintRow("2. In-place Update",        inPlaceTime.TotalSeconds,     inPlaceVal,   inPlaceVal == expected);
        PrintRow("3. Row-level Locking",      rowLockTime.TotalSeconds,     rowLockVal,   rowLockVal == expected);
        PrintRow("4. Optimistic Concurrency", optimisticTime.TotalSeconds,  optimisticVal, optimisticVal == expected);
    }

    static void PrintRow(string name, double sec, int val, bool correct)
    {
        string tick = correct ? "✓" : "✗";
        Console.WriteLine($"{name,-35} {sec,10:F2} {val,12} {tick,12}");
    }
}