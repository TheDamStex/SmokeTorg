using System.Security.Cryptography;
using SmokeTorg.Application.Interfaces;

namespace SmokeTorg.Application.Services;

public class PasswordHasher : IPasswordHasher
{
    public (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var passwordHash = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), 100000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(passwordHash) == hash;
    }
}
