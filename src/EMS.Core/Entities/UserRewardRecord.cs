using EMS.Core.Entities.Enums;
using System;
using System.Collections.Generic;

namespace EMS.Core.Entities;

public class UserRewardRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string RewardCategoryId { get; set; } = string.Empty;
    public string RewardCategoryName { get; set; } = string.Empty;
    public RewardType RewardType { get; set; } = RewardType.TrainingPoint;
    public string DetailName { get; set; } = string.Empty;
    public double Amount { get; set; } = 1.0;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        return new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "userId", UserId },
            { "studentEmail", StudentEmail },
            { "studentName", StudentName },
            { "eventId", EventId },
            { "eventTitle", EventTitle },
            { "rewardCategoryId", RewardCategoryId },
            { "rewardCategoryName", RewardCategoryName },
            { "rewardType", (int)RewardType },
            { "detailName", DetailName },
            { "amount", Amount },
            { "grantedAt", GrantedAt.ToUniversalTime() }
        };
    }
}
