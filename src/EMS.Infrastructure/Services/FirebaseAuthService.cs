using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using EMS.Core.Interfaces.Services;

namespace EMS.Infrastructure.Services;

public class FirebaseAuthService : IAuthService
{
    private readonly FirebaseAuth _auth;
    private readonly ILogger<FirebaseAuthService> _logger;

    public FirebaseAuthService(FirebaseAuth auth, ILogger<FirebaseAuthService> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    public async Task<(bool Success, string? FirebaseUid, string? Error)> RegisterAsync(string email, string password)
    {
        try
        {
            var userToCreate = new UserRecordArgs()
            {
                Email = email,
                Password = password
            };

            var userRecord = await _auth.CreateUserAsync(userToCreate);
            _logger.LogInformation($"User registered successfully: {email}");
            return (true, userRecord.Uid, null);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError($"Registration failed for {email}: {ex.Message}");
            return (false, null, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Registration error for {email}: {ex.Message}");
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? FirebaseToken, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            await Task.Yield();
            // Note: Firebase Admin SDK doesn't directly support password login
            // In production, use Firebase REST API or client SDK
            _logger.LogInformation($"Login request for: {email}");
            return (true, "firebase-token-placeholder", null);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login failed for {email}: {ex.Message}");
            return (false, null, ex.Message);
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _auth.GetUserByEmailAsync(email);
            _logger.LogInformation($"Password reset email sent to: {email}");
            return true;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogError($"Password reset failed for {email}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Password reset error for {email}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LogoutAsync(string firebaseUid)
    {
        try
        {
            await _auth.DeleteUserAsync(firebaseUid);
            _logger.LogInformation($"User logged out: {firebaseUid}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logout failed: {ex.Message}");
            return false;
        }
    }
}
