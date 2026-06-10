using EMS.Core.Entities.Enums;

namespace EMS.Core.Entities;

public class Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public RoleType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "type", (int)Type },
            { "name", Name },
            { "description", Description },
            { "tenantId", TenantId },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt }
        };
    }
}
