using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string? FirebaseUid, string? Error)> RegisterAsync(string email, string password);
    Task<(bool Success, string? FirebaseToken, string? Error)> LoginAsync(string email, string password);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> LogoutAsync(string firebaseUid);
}
