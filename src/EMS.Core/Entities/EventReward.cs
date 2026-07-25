using System;
using System.Collections.Generic;

namespace EMS.Core.Entities;

public class EventReward
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RewardCategoryId { get; set; } = string.Empty;
    public string DetailName { get; set; } = string.Empty; // e.g., "+5 ĐRL", "Áo phông EMS 2026"
    public double ValueOrQuantity { get; set; } = 1.0; // Point value or item quantity
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "eventId", EventId },
            { "tenantId", TenantId },
            { "rewardCategoryId", RewardCategoryId },
            { "detailName", DetailName },
            { "valueOrQuantity", ValueOrQuantity },
            { "description", Description },
            { "createdAt", CreatedAt.ToUniversalTime() },
            { "updatedAt", UpdatedAt.ToUniversalTime() }
        };
    }
}
