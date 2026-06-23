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

            var user = MapToUser(doc);
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

            return MapToUser(snapshot.Documents[0]);
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
                .SetAsync(user.ToFirestoreDocument());

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
                .SetAsync(user.ToFirestoreDocument());

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
                .Select(d => MapToUser(d))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting users by tenant: {ex.Message}");
            return new List<User>();
        }
    }

    private User MapToUser(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();
        
        var user = new User
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            FirebaseUid = dict.TryGetValue("firebaseUid", out var fuid) ? fuid?.ToString() ?? "" : "",
            Email = dict.TryGetValue("email", out var email) ? email?.ToString() ?? "" : "",
            FullName = dict.TryGetValue("fullName", out var name) ? name?.ToString() ?? "" : "",
            PhoneNumber = dict.TryGetValue("phoneNumber", out var phone) ? phone?.ToString() : null,
            MSSV = dict.TryGetValue("mssv", out var mssv) ? mssv?.ToString() : null,
            Department = dict.TryGetValue("department", out var dept) ? dept?.ToString() : null,
            TenantId = dict.TryGetValue("tenantId", out var tenant) ? tenant?.ToString() ?? "" : "",
            Status = dict.TryGetValue("status", out var status) && status is long statusInt ? (EMS.Core.Entities.Enums.UserStatus)statusInt : EMS.Core.Entities.Enums.UserStatus.Active,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow,
            LastLoginAt = dict.TryGetValue("lastLoginAt", out var login) && login is Timestamp loginTs ? (loginTs.ToDateTime() == DateTime.MinValue ? null : (DateTime?)loginTs.ToDateTime()) : null
        };

        if (dict.TryGetValue("roleIds", out var rolesObj) && rolesObj is System.Collections.IEnumerable rolesEnum)
        {
            foreach (var r in rolesEnum)
            {
                var roleStr = r?.ToString();
                if (!string.IsNullOrEmpty(roleStr))
                {
                    user.RoleIds.Add(roleStr);
                }
            }
        }

        return user;
    }
}
