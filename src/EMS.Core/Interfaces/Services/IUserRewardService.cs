using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Core.Interfaces.Services;

public class StudentRewardSummaryDto
{
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public double TotalAmount { get; set; }
    public int RecordCount { get; set; }
    public List<UserRewardRecord> Records { get; set; } = new();
}

public interface IUserRewardService
{
    Task<bool> GrantRewardsOnCheckInAsync(string tenantId, string eventId, string userId, string studentEmail, string studentName);
    Task<List<UserRewardRecord>> GetUserRewardsAsync(string studentEmail, string tenantId, RewardType? type = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(string tenantId, string? rewardCategoryId = null, RewardType? rewardType = null, DateTime? fromDate = null, DateTime? toDate = null, string sortOrder = "desc", string? searchKeyword = null);
}
