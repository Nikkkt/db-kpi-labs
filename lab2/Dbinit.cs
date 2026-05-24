using Npgsql;

namespace Lab2
{
    public static class DbInit
    {
        public static async Task InitializeAsync(string connectionString)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS user_counter (
                    user_id INTEGER PRIMARY KEY,
                    counter INTEGER NOT NULL DEFAULT 0,
                    version INTEGER NOT NULL DEFAULT 0
                );

                INSERT INTO user_counter (user_id, counter, version)
                VALUES (1, 0, 0)
                ON CONFLICT (user_id) DO NOTHING;
            ";
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ResetCounterAsync(string connectionString)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE user_counter SET counter = 0, version = 0 WHERE user_id = 1";
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<int> GetCounterAsync(string connectionString)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT counter FROM user_counter WHERE user_id = 1";
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
    }
}