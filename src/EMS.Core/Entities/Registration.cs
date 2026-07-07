using EMS.Core.Entities.Enums;

namespace EMS.Core.Entities;

public class Registration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    // Check-in tracking
    public bool CheckedIn { get; set; } = false;
    public DateTime? CheckedInAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        var doc = new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "eventId", EventId },
            { "studentEmail", StudentEmail },
            { "studentName", StudentName },
            { "status", (int)Status },
            { "checkedIn", CheckedIn },
            { "createdAt", CreatedAt.ToUniversalTime() },
            { "updatedAt", UpdatedAt.ToUniversalTime() }
        };

        if (CheckedInAt.HasValue)
            doc["checkedInAt"] = CheckedInAt.Value.ToUniversalTime();

        return doc;
    }
}
