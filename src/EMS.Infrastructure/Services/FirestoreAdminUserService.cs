using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;

namespace EMS.Infrastructure.Services;

/// <summary>
/// Firestore implementation của IAdminUserService.
/// Hỗ trợ truy vấn cross-tenant, phân trang, soft delete, thay đổi role/tenant.
/// </summary>
public class FirestoreAdminUserService : IAdminUserService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreAdminUserService> _logger;
    private const string UsersCollection = "users";

    public FirestoreAdminUserService(FirestoreDb firestoreDb, ILogger<FirestoreAdminUserService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<(List<User> Users, int TotalCount)> GetUsersAsync(
        string? tenantId, string? search, string? roleId, int? status, int page, int pageSize)
    {
        try
        {
            Query query = _firestoreDb.Collection(UsersCollection);

            // Filter by tenant if provided (TenantAdmin case)
            if (!string.IsNullOrEmpty(tenantId))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            // Filter by status if provided
            if (status.HasValue)
            {
                query = query.WhereEqualTo("status", status.Value);
            }

            var snapshot = await query.GetSnapshotAsync();
            var allUsers = snapshot.Documents.Select(d => MapToUser(d)).ToList();

            // Client-side filtering for search (Firestore doesn't support LIKE queries)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                allUsers = allUsers.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(u.MSSV) && u.MSSV.ToLowerInvariant().Contains(searchLower))
                ).ToList();
            }

            // Client-side filtering for role (roleIds is an array in Firestore)
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                var targetRole = roleId.Trim();
                allUsers = allUsers.Where(u => u.RoleIds.Any(r =>
                    r.Equals(targetRole, StringComparison.OrdinalIgnoreCase) ||
                    (targetRole.Equals("employee", StringComparison.OrdinalIgnoreCase) && r.Equals("student", StringComparison.OrdinalIgnoreCase)) ||
                    (targetRole.Equals("student", StringComparison.OrdinalIgnoreCase) && r.Equals("employee", StringComparison.OrdinalIgnoreCase))
                )).ToList();
            }

            // Sort by creation date descending
            allUsers = allUsers.OrderByDescending(u => u.CreatedAt).ToList();

            var totalCount = allUsers.Count;

            // Pagination
            var pagedUsers = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedUsers, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin user list");
            return (new List<User>(), 0);
        }
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        try
        {
            var doc = await _firestoreDb
                .Collection(UsersCollection)
                .Document(userId)
                .GetSnapshotAsync();

            if (!doc.Exists) return null;

            return MapToUser(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} for admin", userId);
            return null;
        }
    }

    public async Task<bool> SoftDeleteUserAsync(string userId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(UsersCollection).Document(userId);
            var doc = await docRef.GetSnapshotAsync();
            if (!doc.Exists) return false;

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "status", (int)UserStatus.Inactive },
                { "updatedAt", DateTime.UtcNow }
            });

            _logger.LogInformation("User {UserId} soft-deleted", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft-deleting user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ToggleUserActiveAsync(string userId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(UsersCollection).Document(userId);
            var doc = await docRef.GetSnapshotAsync();
            if (!doc.Exists) return false;

            var user = MapToUser(doc);
            var newStatus = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "status", (int)newStatus },
                { "updatedAt", DateTime.UtcNow }
            });

            _logger.LogInformation("User {UserId} status toggled to {Status}", userId, newStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user {UserId} active status", userId);
            return false;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string roleId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(UsersCollection).Document(userId);
            var doc = await docRef.GetSnapshotAsync();
            if (!doc.Exists) return false;

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "roleIds", new List<string> { roleId } },
                { "updatedAt", DateTime.UtcNow }
            });

            _logger.LogInformation("User {UserId} role changed to {RoleId}", userId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> UpdateUserTenantAsync(string userId, string tenantId)
    {
        try
        {
            var docRef = _firestoreDb.Collection(UsersCollection).Document(userId);
            var doc = await docRef.GetSnapshotAsync();
            if (!doc.Exists) return false;

            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "tenantId", tenantId },
                { "updatedAt", DateTime.UtcNow }
            });

            _logger.LogInformation("User {UserId} moved to tenant {TenantId}", userId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing tenant for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Map Firestore document snapshot sang User entity.
    /// Dùng chung logic với FirestoreUserService.
    /// </summary>
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
            Status = dict.TryGetValue("status", out var statusVal) && statusVal is long statusInt
                ? (UserStatus)statusInt
                : UserStatus.Active,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs
                ? createdTs.ToDateTime()
                : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs
                ? updatedTs.ToDateTime()
                : DateTime.UtcNow,
            LastLoginAt = dict.TryGetValue("lastLoginAt", out var login) && login is Timestamp loginTs
                ? (loginTs.ToDateTime() == DateTime.MinValue ? null : (DateTime?)loginTs.ToDateTime())
                : null
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
