using MySqlConnector;
using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Infrastructure.Services;

public class MySqlConnectionFactory(IConnectionStringProvider connectionStringProvider) : IMySqlConnectionFactory
{
    public async Task<MySqlConnection> CreateOpenConnectionAsync()
    {
        var connection = new MySqlConnection(await connectionStringProvider.GetConnectionStringAsync());
        await connection.OpenAsync();
        return connection;
    }
}
