using System.Diagnostics;
using Npgsql;

namespace lab2;

public static class InPlaceUpdate
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
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE user_counter SET counter = counter + 1 WHERE user_id = @id";
                    cmd.Parameters.AddWithValue("id", 1);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        return sw.Elapsed;
    }
}