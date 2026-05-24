using System.Diagnostics;
using Npgsql;

namespace lab2;

public static class RowLevelLocking
{
    public static async Task<TimeSpan> RunAsync(string connectionString, int threads, int iterations)
    {
        var tasks = new Task[threads];
        var sw = Stopwatch.StartNew();

        for (int t = 0; t < threads; t++)
        {
            tasks[t] = Task.Run(async () =>
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                for (int i = 0; i < iterations; i++)
                {
                    await using var tx = await conn.BeginTransactionAsync();

                    int counter;
                    await using (var selectCmd = conn.CreateCommand())
                    {
                        selectCmd.Transaction = tx;
                        selectCmd.CommandText = "SELECT counter FROM user_counter WHERE user_id = 1 FOR UPDATE";
                        counter = (int)(await selectCmd.ExecuteScalarAsync())!;
                    }

                    counter++;

                    await using (var updateCmd = conn.CreateCommand())
                    {
                        updateCmd.Transaction = tx;
                        updateCmd.CommandText = "UPDATE user_counter SET counter = @counter WHERE user_id = @id";
                        updateCmd.Parameters.AddWithValue("counter", counter);
                        updateCmd.Parameters.AddWithValue("id", 1);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                }
            });
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        return sw.Elapsed;
    }
}