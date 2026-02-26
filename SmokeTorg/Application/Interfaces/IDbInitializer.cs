namespace SmokeTorg.Application.Interfaces;

public interface IDbInitializer
{
    Task TestConnectionAsync(string connectionString);
    Task EnsureDatabaseAsync(string serverConnectionString, string dbName);
    Task EnsureSchemaAsync(string connectionString);
    Task<int> GetSchemaVersionAsync(string connectionString);
    Task SetSchemaVersionAsync(string connectionString, int version);
}
