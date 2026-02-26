using SmokeTorg.Domain.Entities;
using SmokeTorg.Domain.Enums;

namespace SmokeTorg.Application.Interfaces;

public interface IUserService
{
    Task<User> CreateUserAsync(string username, string password, UserRole role, string? fullName = null);
    Task UpdateUserRoleAsync(Guid userId, UserRole role);
    Task SetUserActiveAsync(Guid userId, bool isActive);
    Task ResetPasswordAsync(Guid userId, string newPassword);
    Task<List<User>> GetUsersAsync(UserRole? role = null, string? search = null);
}
