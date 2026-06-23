using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string userId, string tenantId);
    Task<User?> GetUserByEmailAsync(string email, string tenantId);
    Task<User?> GetUserByEmailGlobalAsync(string email);
    Task<User?> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string userId, string tenantId);
    Task<List<User>> GetUsersByTenantAsync(string tenantId);
}
