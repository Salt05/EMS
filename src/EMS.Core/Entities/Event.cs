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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "title", Title },
            { "description", Description },
            { "location", Location },
            { "venueId", VenueId ?? "" },
            { "startTime", StartTime },
            { "endTime", EndTime },
            { "capacity", Capacity },
            { "imageUrl", ImageUrl ?? "" },
            { "organizerId", OrganizerId },
            { "status", (int)Status },
            { "approvedById", ApprovedById ?? "" },
            { "approvedAt", ApprovedAt ?? DateTime.MinValue },
            { "rejectionReason", RejectionReason ?? "" },
            { "createdAt", CreatedAt },
            { "updatedAt", UpdatedAt }
        };
    }
}
