namespace SmokeTorg.Application.Interfaces;

public interface IDbInitializer
{
    Task<OperationResult> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<OperationResult> EnsureDatabaseAsync(string serverConnectionString, string dbName, CancellationToken cancellationToken = default);
    Task<OperationResult> EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken = default);
    Task<int> GetSchemaVersionAsync(string connectionString);
    Task SetSchemaVersionAsync(string connectionString, int version);
}

public sealed class OperationResult
{
    public bool Success { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Exception? DebugException { get; init; }

    public static OperationResult Ok(string code, string message) =>
        new() { Success = true, Code = code, Message = message };

    public static OperationResult Fail(string code, string message, Exception? debugException = null) =>
        new() { Success = false, Code = code, Message = message, DebugException = debugException };
}
