using System.Security.Cryptography;
using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;

namespace SmokeTorg.Application.Services;

public class AuthService(IUserRepository userRepository)
{
    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await userRepository.GetByUsernameAsync(username);
        if (user is null || !user.IsActive) return null;
        var hash = HashPassword(password, Convert.FromBase64String(user.Salt));
        return hash == user.PasswordHash ? user : null;
    }

    public static (string Hash, string Salt) CreateHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return (HashPassword(password, salt), Convert.ToBase64String(salt));
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var bytes = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(bytes);
    }
}
