using MySqlConnector;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Infrastructure.Repositories.Sql;

public class MySqlUserRepository(IMySqlConnectionFactory connectionFactory) : IUserRepository
{
    public async Task<List<User>> GetAllAsync() => await FilterAsync(null, null);

    public async Task<User?> GetByIdAsync(Guid id)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users WHERE Id=@id LIMIT 1;";
        command.Parameters.AddWithValue("@id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task AddAsync(User entity)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Users (Id, Username, PasswordHash, PasswordSalt, Role, IsActive, FullName, CreatedAt)
VALUES (@id, @username, @hash, @salt, @role, @isActive, @fullName, @createdAt);";
        FillParameters(command, entity);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(User entity)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE Users SET Username=@username, PasswordHash=@hash, PasswordSalt=@salt, Role=@role,
IsActive=@isActive, FullName=@fullName WHERE Id=@id;";
        FillParameters(command, entity);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Users WHERE Id=@id;";
        command.Parameters.AddWithValue("@id", id.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users WHERE Username=@username LIMIT 1;";
        command.Parameters.AddWithValue("@username", username);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> UsernameExistsAsync(string username) => await GetByUsernameAsync(username) is not null;

    public async Task<List<User>> FilterAsync(string? search, UserRole? role)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Users
WHERE (@search IS NULL OR Username LIKE CONCAT('%', @search, '%'))
  AND (@role IS NULL OR Role = @role)
ORDER BY Username;";
        command.Parameters.AddWithValue("@search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);
        command.Parameters.AddWithValue("@role", role is null ? DBNull.Value : (int)role.Value);

        var users = new List<User>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) users.Add(Map(reader));
        return users;
    }

    private static User Map(MySqlDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString("Id")),
        Username = reader.GetString("Username"),
        PasswordHash = reader.GetString("PasswordHash"),
        Salt = reader.GetString("PasswordSalt"),
        Role = (UserRole)reader.GetInt32("Role"),
        IsActive = reader.GetBoolean("IsActive"),
        FullName = reader["FullName"] as string ?? string.Empty,
        CreatedAt = reader.GetDateTime("CreatedAt")
    };

    private static void FillParameters(MySqlCommand command, User entity)
    {
        command.Parameters.AddWithValue("@id", entity.Id.ToString());
        command.Parameters.AddWithValue("@username", entity.Username);
        command.Parameters.AddWithValue("@hash", entity.PasswordHash);
        command.Parameters.AddWithValue("@salt", entity.Salt);
        command.Parameters.AddWithValue("@role", (int)entity.Role);
        command.Parameters.AddWithValue("@isActive", entity.IsActive);
        command.Parameters.AddWithValue("@fullName", string.IsNullOrWhiteSpace(entity.FullName) ? DBNull.Value : entity.FullName);
        command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
    }
}
