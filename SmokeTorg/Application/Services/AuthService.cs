using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Application.Services;

public record AuthSession(Guid UserId, string Username, UserRole Role, string FullName, bool IsActive);

public class AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
{
    public AuthSession? CurrentSession { get; private set; }

    public async Task<AuthSession?> LoginAsync(string username, string password)
    {
        var user = await userRepository.GetByUsernameAsync(username);
        if (user is null || !user.IsActive) return null;
        if (!passwordHasher.Verify(password, user.PasswordHash, user.Salt)) return null;

        CurrentSession = new AuthSession(user.Id, user.Username, user.Role, user.FullName, user.IsActive);
        return CurrentSession;
    }

    public void Logout() => CurrentSession = null;
}
