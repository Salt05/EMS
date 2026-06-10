namespace EMS.Core.Entities;

public class Tenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // For Firestore serialization
    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "name", Name },
            { "subdomain", Subdomain },
            { "email", Email },
            { "phoneNumber", PhoneNumber ?? "" },
            { "address", Address ?? "" },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt },
            { "isActive", IsActive }
        };
    }
}
