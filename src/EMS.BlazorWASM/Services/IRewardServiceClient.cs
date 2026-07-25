using EMS.Shared.DTOs.Rewards;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.BlazorWASM.Services;

public interface IRewardServiceClient
{
    Task<List<RewardCategoryDto>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true);
    Task<RewardCategoryDto?> GetCategoryByIdAsync(string id, string tenantId);
    Task<RewardCategoryDto> CreateCategoryAsync(RewardCategoryDto category);
    Task<bool> UpdateCategoryAsync(RewardCategoryDto category);
    Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive);

    Task<List<EventRewardDto>> GetRewardsByEventAsync(string eventId, string tenantId);
    Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventRewardDto> rewards);

    Task<List<StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(string tenantId, string? rewardCategoryId = null, RewardTypeDto? rewardType = null, DateTime? fromDate = null, DateTime? toDate = null, string sortOrder = "desc", string? searchKeyword = null);
}
