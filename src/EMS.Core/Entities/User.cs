using EMS.Core.Entities.Enums;

namespace EMS.Core.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseUid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? MSSV { get; set; }
    public string? Department { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public List<string> RoleIds { get; set; } = new();
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "firebaseUid", FirebaseUid },
            { "email", Email },
            { "fullName", FullName },
            { "phoneNumber", PhoneNumber ?? "" },
            { "mssv", MSSV ?? "" },
            { "department", Department ?? "" },
            { "tenantId", TenantId },
            { "roleIds", RoleIds },
            { "status", (int)Status },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt },
            { "lastLoginAt", LastLoginAt ?? DateTime.MinValue }
        };
    }
}
