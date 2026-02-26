using MySqlConnector;

namespace SmokeTorg.Application.Interfaces;

public interface IMySqlConnectionFactory
{
    Task<MySqlConnection> CreateOpenConnectionAsync();
}
