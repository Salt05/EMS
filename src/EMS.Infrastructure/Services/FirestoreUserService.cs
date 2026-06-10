using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;

namespace EMS.Infrastructure.Services;

public class FirestoreUserService : IUserService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreUserService> _logger;
    private const string UsersCollection = "users";

    public FirestoreUserService(FirestoreDb firestoreDb, ILogger<FirestoreUserService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string userId, string tenantId)
    {
        try
        {
            var doc = await _firestoreDb
                .Collection(UsersCollection)
                .Document(userId)
                .GetSnapshotAsync();

            if (!doc.Exists) return null;

            var user = doc.ConvertTo<User>();
            if (user.TenantId != tenantId) return null;

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting user {userId}: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email, string tenantId)
    {
        try
        {
            var query = _firestoreDb
                .Collection(UsersCollection)
                .WhereEqualTo("email", email)
                .WhereEqualTo("tenantId", tenantId);

            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0) return null;

            return snapshot.Documents[0].ConvertTo<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting user by email {email}: {ex.Message}");
            return null;
        }
    }

    public async Task<User?> CreateUserAsync(User user)
    {
        try
        {
            await _firestoreDb
                .Collection(UsersCollection)
                .Document(user.Id)
                .SetAsync(user);

            _logger.LogInformation($"User created: {user.Email}");
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating user: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _firestoreDb
                .Collection(UsersCollection)
                .Document(user.Id)
                .SetAsync(user);

            _logger.LogInformation($"User updated: {user.Email}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating user: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId, string tenantId)
    {
        try
        {
            await _firestoreDb
                .Collection(UsersCollection)
                .Document(userId)
                .DeleteAsync();

            _logger.LogInformation($"User deleted: {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting user: {ex.Message}");
            return false;
        }
    }

    public async Task<List<User>> GetUsersByTenantAsync(string tenantId)
    {
        try
        {
            var query = _firestoreDb
                .Collection(UsersCollection)
                .WhereEqualTo("tenantId", tenantId);

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents
                .Select(d => d.ConvertTo<User>())
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting users by tenant: {ex.Message}");
            return new List<User>();
        }
    }
}
