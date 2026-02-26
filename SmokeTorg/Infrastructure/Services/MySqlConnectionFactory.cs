using MySqlConnector;
using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Infrastructure.Services;

public class MySqlConnectionFactory(IDbSettingsService settingsService) : IMySqlConnectionFactory
{
    public async Task<MySqlConnection> CreateOpenConnectionAsync()
    {
        var settings = await settingsService.LoadAsync();
        var connection = new MySqlConnection(settingsService.GetConnectionString(settings));
        await connection.OpenAsync();
        return connection;
    }
}
