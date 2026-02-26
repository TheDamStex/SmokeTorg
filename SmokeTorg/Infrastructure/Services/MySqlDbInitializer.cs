using MySqlConnector;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Infrastructure.SqlScripts;

namespace SmokeTorg.Infrastructure.Services;

public class MySqlDbInitializer : IDbInitializer
{
    public async Task TestConnectionAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.CloseAsync();
    }

    public async Task EnsureDatabaseAsync(string serverConnectionString, string dbName)
    {
        await using var connection = new MySqlConnection(serverConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        await command.ExecuteNonQueryAsync();
    }

    public async Task EnsureSchemaAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var sql in MySqlSchemaScripts.CreateTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        await SetSchemaVersionAsync(connectionString, MySqlSchemaScripts.CurrentVersion);
    }

    public async Task<int> GetSchemaVersionAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SchemaVersion FROM SchemaInfo ORDER BY Id DESC LIMIT 1;";
        var result = await command.ExecuteScalarAsync();
        return result is null ? 0 : Convert.ToInt32(result);
    }

    public async Task SetSchemaVersionAsync(string connectionString, int version)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO SchemaInfo (SchemaVersion, AppliedAt) VALUES (@version, @appliedAt);";
        command.Parameters.AddWithValue("@version", version);
        command.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync();
    }
}
