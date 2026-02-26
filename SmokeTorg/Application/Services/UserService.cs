using SmokeTorg.Application.Interfaces;
using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Application.Services;

public class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher) : IUserService
{
    public async Task<User> CreateUserAsync(string username, string password, UserRole role, string? fullName = null)
    {
        if (password.Length < 6) throw new InvalidOperationException("Пароль має містити щонайменше 6 символів.");
        if (await userRepository.UsernameExistsAsync(username)) throw new InvalidOperationException("Логін вже існує.");

        var (hash, salt) = passwordHasher.HashPassword(password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            FullName = fullName ?? string.Empty
        };
        await userRepository.AddAsync(user);
        return user;
    }

    public async Task UpdateUserRoleAsync(Guid userId, UserRole role)
    {
        var user = await userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        user.Role = role;
        await userRepository.UpdateAsync(user);
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive)
    {
        var user = await userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        user.IsActive = isActive;
        await userRepository.UpdateAsync(user);
    }

    public async Task ResetPasswordAsync(Guid userId, string newPassword)
    {
        if (newPassword.Length < 6) throw new InvalidOperationException("Пароль має містити щонайменше 6 символів.");
        var user = await userRepository.GetByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        var (hash, salt) = passwordHasher.HashPassword(newPassword);
        user.PasswordHash = hash;
        user.Salt = salt;
        await userRepository.UpdateAsync(user);
    }

    public Task<List<User>> GetUsersAsync(UserRole? role = null, string? search = null) => userRepository.FilterAsync(search, role);
}
