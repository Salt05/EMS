using System;
using System.Collections.Generic;

namespace EMS.Core.Entities;

public class AgendaItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "eventId", EventId },
            { "startTime", StartTime.ToUniversalTime() },
            { "endTime", EndTime.ToUniversalTime() },
            { "title", Title },
            { "createdAt", CreatedAt.ToUniversalTime() },
            { "updatedAt", UpdatedAt.ToUniversalTime() }
        };
    }
}
