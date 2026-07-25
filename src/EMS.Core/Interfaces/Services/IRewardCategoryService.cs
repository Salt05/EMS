using EMS.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Core.Interfaces.Services;

public interface IRewardCategoryService
{
    Task<List<RewardCategory>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true);
    Task<RewardCategory?> GetCategoryByIdAsync(string id, string tenantId);
    Task<RewardCategory> CreateCategoryAsync(RewardCategory category);
    Task<bool> UpdateCategoryAsync(RewardCategory category);
    Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive);
}
