namespace SmokeTorg.Application.Interfaces;

public record DbSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 3306;
    public string Database { get; init; } = "smoketorg";
    public string User { get; init; } = "root";
    public string Password { get; init; } = string.Empty;
    public DbSslMode SslMode { get; init; } = DbSslMode.Preferred;
    public bool AllowPublicKeyRetrieval { get; init; } = true;
    public bool IsConfigured { get; init; }
}

public enum DbSslMode
{
    None,
    Preferred,
    Required
}

public interface IDbSettingsService
{
    Task<DbSettings> LoadAsync();
    Task SaveAsync(DbSettings settings);
    string GetConnectionString(DbSettings settings);
    string GetServerConnectionString(DbSettings settings);
    Task<bool> IsConfiguredAsync();
}
