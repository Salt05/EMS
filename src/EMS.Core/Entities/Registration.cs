using EMS.Core.Entities.Enums;

namespace EMS.Core.Entities;

public class Registration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Approval / rejection tracking
    public string? ProcessedById { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Cancellation tracking
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "eventId", EventId },
            { "userId", UserId },
            { "note", Note ?? "" },
            { "status", (int)Status },
            { "registeredAt", RegisteredAt },
            { "processedById", ProcessedById ?? "" },
            { "processedAt", ProcessedAt ?? DateTime.MinValue },
            { "rejectionReason", RejectionReason ?? "" },
            { "cancelledAt", CancelledAt ?? DateTime.MinValue },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt }
        };
    }
}
