using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Infrastructure.Services;

public class ConnectionStringProvider(IDbSettingsService settingsService) : IConnectionStringProvider
{
    private DbSettings? _runtimeSettings;

    public void SetRuntimeSettings(DbSettings settings)
    {
        _runtimeSettings = settings;
    }

    public void ClearRuntimeSettings()
    {
        _runtimeSettings = null;
    }

    public DbSettings? GetRuntimeSettings() => _runtimeSettings;

    public async Task<string> GetConnectionStringAsync()
    {
        var settings = _runtimeSettings ?? await settingsService.LoadAsync();
        return settingsService.GetConnectionString(settings);
    }

    public async Task<string> GetServerConnectionStringAsync()
    {
        var settings = _runtimeSettings ?? await settingsService.LoadAsync();
        return settingsService.GetServerConnectionString(settings);
    }

    public string GetConnectionString(DbSettings settings) => settingsService.GetConnectionString(settings);

    public string GetServerConnectionString(DbSettings settings) => settingsService.GetServerConnectionString(settings);
}
