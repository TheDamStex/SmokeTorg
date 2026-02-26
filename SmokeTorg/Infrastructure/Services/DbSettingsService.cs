using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MySqlConnector;
using SmokeTorg.Application.Interfaces;
using System.IO;

namespace SmokeTorg.Infrastructure.Services;

public class DbSettingsService : IDbSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public DbSettingsService()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "appsettings.local.json");
    }

    public async Task<DbSettings> LoadAsync()
    {
        if (!File.Exists(_path)) return new DbSettings { IsConfigured = false };

        var dto = JsonSerializer.Deserialize<DbSettingsDto>(await File.ReadAllTextAsync(_path)) ?? new DbSettingsDto();
        return new DbSettings
        {
            Host = dto.Host,
            Port = dto.Port,
            Database = dto.Database,
            User = dto.User,
            Password = Unprotect(dto.Password),
            UseSsl = dto.UseSsl,
            AllowPublicKeyRetrieval = dto.AllowPublicKeyRetrieval,
            IsConfigured = dto.IsConfigured
        };
    }

    public async Task SaveAsync(DbSettings settings)
    {
        var dto = new DbSettingsDto
        {
            Host = settings.Host,
            Port = settings.Port,
            Database = settings.Database,
            User = settings.User,
            Password = Protect(settings.Password),
            UseSsl = settings.UseSsl,
            AllowPublicKeyRetrieval = settings.AllowPublicKeyRetrieval,
            IsConfigured = settings.IsConfigured
        };
        await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    public string GetConnectionString(DbSettings settings)
    {
        var csb = new MySqlConnectionStringBuilder
        {
            Server = settings.Host,
            Port = (uint)settings.Port,
            Database = settings.Database,
            UserID = settings.User,
            Password = settings.Password,
            SslMode = settings.UseSsl ? MySqlSslMode.Required : MySqlSslMode.None,
            AllowPublicKeyRetrieval = settings.AllowPublicKeyRetrieval
        };
        return csb.ConnectionString;
    }

    public string GetServerConnectionString(DbSettings settings)
    {
        var csb = new MySqlConnectionStringBuilder(GetConnectionString(settings)) { Database = string.Empty };
        return csb.ConnectionString;
    }

    public async Task<bool> IsConfiguredAsync() => (await LoadAsync()).IsConfigured;

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));
    }

    private static string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var bytes = Convert.FromBase64String(value);
        var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotected);
    }

    private sealed class DbSettingsDto
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = "smoketorg";
        public string User { get; set; } = "root";
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; }
        public bool AllowPublicKeyRetrieval { get; set; } = true;
        public bool IsConfigured { get; set; }
    }
}
