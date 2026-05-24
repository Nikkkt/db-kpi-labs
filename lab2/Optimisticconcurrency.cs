using System.Diagnostics;
using Npgsql;

namespace lab2;

public static class OptimisticConcurrency
{
    public static async Task<TimeSpan> RunAsync(string connectionString, int threads, int iterations)
    {
        long totalRetries = 0;
        var tasks = new Task[threads];
        var sw = Stopwatch.StartNew();

        for (int t = 0; t < threads; t++)
        {
            tasks[t] = Task.Run(async () =>
            {
                long localRetries = 0;
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                for (int i = 0; i < iterations; i++)
                {
                    bool success = false;
                    while (!success)
                    {
                        int counter, version;
                        await using (var selectCmd = conn.CreateCommand())
                        {
                            selectCmd.CommandText = "SELECT counter, version FROM user_counter WHERE user_id = 1";
                            await using var reader = await selectCmd.ExecuteReaderAsync();
                            await reader.ReadAsync();
                            counter = reader.GetInt32(0);
                            version = reader.GetInt32(1);
                        }

                        counter++;

                        await using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.CommandText = @"
                                    UPDATE user_counter
                                    SET counter = @counter, version = @newVersion
                                    WHERE user_id = @id AND version = @oldVersion";
                            updateCmd.Parameters.AddWithValue("counter", counter);
                            updateCmd.Parameters.AddWithValue("newVersion", version + 1);
                            updateCmd.Parameters.AddWithValue("id", 1);
                            updateCmd.Parameters.AddWithValue("oldVersion", version);

                            int affected = await updateCmd.ExecuteNonQueryAsync();
                            if (affected > 0) success = true;
                            else localRetries++;
                        }
                    }
                }

                Interlocked.Add(ref totalRetries, localRetries);
            });
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        Console.WriteLine($"  Кількість повторних спроб (retries): {totalRetries}");
        return sw.Elapsed;
    }
}