using MySqlConnector;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Infrastructure.SqlScripts;

namespace SmokeTorg.Infrastructure.Services;

public class MySqlDbInitializer : IDbInitializer
{
    public async Task<OperationResult> TestServerConnectionAsync(string serverConnectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = 10;
            await command.ExecuteScalarAsync(cancellationToken);

            return OperationResult.Ok("DB_SERVER_CONNECTION_OK", "Підключення до сервера MySQL успішне.");
        }
        catch (Exception ex)
        {
            return MapError(ex);
        }
    }

    public async Task<OperationResult> TestDatabaseExistsAsync(string serverConnectionString, string dbName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dbName))
        {
            return OperationResult.Fail("DB_NAME_INVALID", "Некоректна назва бази даних.");
        }

        try
        {
            await using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @db;";
            command.CommandTimeout = 10;
            command.Parameters.AddWithValue("@db", dbName.Trim());

            var schemaName = await command.ExecuteScalarAsync(cancellationToken);
            if (schemaName is null)
            {
                return OperationResult.Fail("DB_NOT_FOUND", "База даних не знайдена. Перейдіть до кроку 2 для створення.");
            }

            return OperationResult.Ok("DB_EXISTS", "Базу даних знайдено.");
        }
        catch (Exception ex)
        {
            return MapError(ex);
        }
    }

    public async Task<OperationResult> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        return await TestServerConnectionAsync(connectionString, cancellationToken);
    }

    public async Task<OperationResult> EnsureDatabaseAsync(string serverConnectionString, string dbName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dbName))
        {
            return OperationResult.Fail("DB_NAME_INVALID", "Некоректна назва бази даних.");
        }

        var normalizedName = dbName.Trim().Replace("`", "``", StringComparison.Ordinal);

        try
        {
            await using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{normalizedName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            command.CommandTimeout = 30;
            await command.ExecuteNonQueryAsync(cancellationToken);

            return OperationResult.Ok("DB_CREATED_OR_EXISTS", "Базу даних створено або вона вже існує.");
        }
        catch (Exception ex)
        {
            return MapError(ex, "Для створення бази потрібна привілея CREATE DATABASE.");
        }
    }

    public async Task<OperationResult> EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            foreach (var sql in MySqlSchemaScripts.CreateTables)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.CommandTimeout = 30;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var schemaVersionCommand = connection.CreateCommand();
            schemaVersionCommand.Transaction = transaction;
            schemaVersionCommand.CommandText = """
                INSERT INTO SchemaInfo (SchemaVersion, AppliedAt)
                SELECT @version, @appliedAt
                WHERE NOT EXISTS (
                    SELECT 1 FROM SchemaInfo WHERE SchemaVersion = @version
                );
                """;
            schemaVersionCommand.Parameters.AddWithValue("@version", MySqlSchemaScripts.CurrentVersion);
            schemaVersionCommand.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow);
            schemaVersionCommand.CommandTimeout = 30;
            await schemaVersionCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Ok("DB_SCHEMA_READY", "Схему бази даних успішно ініціалізовано.");
        }
        catch (Exception ex)
        {
            return MapError(ex, "Для створення таблиць потрібні привілеї CREATE, ALTER, INDEX.");
        }
    }

    public async Task<int> GetSchemaVersionAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(SchemaVersion), 0) FROM SchemaInfo;";
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

    private static OperationResult MapError(Exception exception, string? permissionHint = null)
    {
        if (exception is MySqlException mysqlException)
        {
            var details = BuildMysqlDetails(mysqlException);
            return mysqlException.Number switch
            {
                1045 => OperationResult.Fail("DB_AUTH_FAILED", $"Доступ заборонено. Причини: невірний логін/пароль АБО користувач не має доступу з цього хоста. {details}", mysqlException),
                1049 => OperationResult.Fail("DB_NOT_FOUND", $"База даних не існує (потрібно створити на кроці 2). {details}", mysqlException),
                1044 or 1142 => OperationResult.Fail("DB_PERMISSION_DENIED", $"Немає прав доступу до бази даних. {(permissionHint ?? string.Empty)} {details}".Trim(), mysqlException),
                1042 or 2003 => OperationResult.Fail("DB_HOST_UNREACHABLE", $"Сервер недоступний (перевірте хост/порт/мережу). {details}", mysqlException),
                1251 or 2054 => OperationResult.Fail("DB_AUTH_PLUGIN", $"Проблема сумісності методу автентифікації (плагін). {details}", mysqlException),
                _ when mysqlException.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                    => OperationResult.Fail("DB_SSL_ERROR", $"Помилка SSL-підключення. Перевірте режим SSL та сертифікати. {details}", mysqlException),
                _ when mysqlException.Message.Contains("Public Key Retrieval", StringComparison.OrdinalIgnoreCase)
                    => OperationResult.Fail("DB_PUBLIC_KEY_RETRIEVAL", $"Сервер вимагає AllowPublicKeyRetrieval. Увімкніть лише для локального підключення без SSL. {details}", mysqlException),
                _ when mysqlException.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                    => OperationResult.Fail("DB_TIMEOUT", $"Час очікування MySQL вичерпано. Перевірте доступність сервера. {details}", mysqlException),
                _ => OperationResult.Fail("DB_UNKNOWN_ERROR", $"Помилка MySQL ({mysqlException.Number}): {GetShortMessage(mysqlException.Message)}. {details}", mysqlException)
            };
        }

        if (exception is OperationCanceledException)
        {
            return OperationResult.Fail("DB_OPERATION_CANCELED", "Операцію скасовано через таймаут або запит користувача.", exception);
        }

        return OperationResult.Fail("DB_GENERAL_ERROR", "Сталася помилка під час роботи з базою даних.", exception);
    }

    private static string BuildMysqlDetails(MySqlException exception)
    {
        return $"[Number={exception.Number}; SqlState={exception.SqlState ?? "n/a"}; Message={GetShortMessage(exception.Message)}]";
    }

    private static string GetShortMessage(string message)
    {
        var sanitized = message.Replace(Environment.NewLine, " ", StringComparison.Ordinal).Trim();
        return sanitized.Length <= 220 ? sanitized : sanitized[..220] + "…";
    }
}
