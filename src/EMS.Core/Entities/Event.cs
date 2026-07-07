using EMS.Core.Entities.Enums;

namespace EMS.Core.Entities;

public class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? VenueId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public string? ImageUrl { get; set; }
    public string OrganizerId { get; set; } = string.Empty;
    public EventStatus Status { get; set; } = EventStatus.Pending;

    // Approval / rejection tracking
    public string? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Check-in code configuration
    public string? CheckInCode { get; set; }
    public DateTime? CheckInCodeExpiredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        var doc = new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "title", Title },
            { "description", Description },
            { "location", Location },
            { "startTime", StartTime.ToUniversalTime() },
            { "endTime", EndTime.ToUniversalTime() },
            { "capacity", Capacity },
            { "organizerId", OrganizerId },
            { "status", (int)Status },
            { "createdAt", CreatedAt.ToUniversalTime() },
            { "updatedAt", UpdatedAt.ToUniversalTime() }
        };

        if (VenueId != null) doc["venueId"] = VenueId;
        if (ImageUrl != null) doc["imageUrl"] = ImageUrl;
        if (ApprovedById != null) doc["approvedById"] = ApprovedById;
        if (ApprovedAt.HasValue) doc["approvedAt"] = ApprovedAt.Value.ToUniversalTime();
        if (RejectionReason != null) doc["rejectionReason"] = RejectionReason;
        if (CheckInCode != null) doc["checkInCode"] = CheckInCode;
        if (CheckInCodeExpiredAt.HasValue) doc["checkInCodeExpiredAt"] = CheckInCodeExpiredAt.Value.ToUniversalTime();

        return doc;
    }
}
