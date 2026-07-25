using EMS.BlazorWASM.Services;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Rewards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Mvc.Services;

public class ServerRewardServiceClient : IRewardServiceClient
{
    private readonly IRewardCategoryService _categoryService;
    private readonly IEventRewardService _eventRewardService;
    private readonly IUserRewardService _userRewardService;

    public ServerRewardServiceClient(
        IRewardCategoryService categoryService,
        IEventRewardService eventRewardService,
        IUserRewardService userRewardService)
    {
        _categoryService = categoryService;
        _eventRewardService = eventRewardService;
        _userRewardService = userRewardService;
    }

    public async Task<List<RewardCategoryDto>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true)
    {
        var list = await _categoryService.GetCategoriesByTenantAsync(tenantId, activeOnly);
        return list.Select(MapCategoryToDto).ToList();
    }

    public async Task<RewardCategoryDto?> GetCategoryByIdAsync(string id, string tenantId)
    {
        var cat = await _categoryService.GetCategoryByIdAsync(id, tenantId);
        return cat == null ? null : MapCategoryToDto(cat);
    }

    public async Task<RewardCategoryDto> CreateCategoryAsync(RewardCategoryDto category)
    {
        var cat = new RewardCategory
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Name = category.Name,
            Type = (RewardType)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        var created = await _categoryService.CreateCategoryAsync(cat);
        return MapCategoryToDto(created);
    }

    public async Task<bool> UpdateCategoryAsync(RewardCategoryDto category)
    {
        var cat = new RewardCategory
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Name = category.Name,
            Type = (RewardType)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        return await _categoryService.UpdateCategoryAsync(cat);
    }

    public async Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive)
    {
        return await _categoryService.ToggleActiveStatusAsync(id, tenantId, isActive);
    }

    public async Task<List<EventRewardDto>> GetRewardsByEventAsync(string eventId, string tenantId)
    {
        var list = await _eventRewardService.GetRewardsByEventAsync(eventId, tenantId);
        return list.Select(r => new EventRewardDto
        {
            Id = r.Id,
            EventId = r.EventId,
            TenantId = r.TenantId,
            RewardCategoryId = r.RewardCategoryId,
            DetailName = r.DetailName,
            ValueOrQuantity = r.ValueOrQuantity,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventRewardDto> rewards)
    {
        var entities = rewards.Select(r => new EventReward
        {
            Id = r.Id,
            EventId = r.EventId,
            TenantId = r.TenantId,
            RewardCategoryId = r.RewardCategoryId,
            DetailName = r.DetailName,
            ValueOrQuantity = r.ValueOrQuantity,
            Description = r.Description
        }).ToList();

        return await _eventRewardService.SaveEventRewardsAsync(eventId, tenantId, entities);
    }

    public async Task<List<EMS.Shared.DTOs.Rewards.StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(
        string tenantId,
        string? rewardCategoryId = null,
        RewardTypeDto? rewardType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortOrder = "desc",
        string? searchKeyword = null)
    {
        RewardType? coreRewardType = rewardType.HasValue ? (RewardType)(int)rewardType.Value : null;
        var coreStats = await _userRewardService.GetTenantUserRewardStatsAsync(tenantId, rewardCategoryId, coreRewardType, fromDate, toDate, sortOrder, searchKeyword);

        return coreStats.Select(s => new EMS.Shared.DTOs.Rewards.StudentRewardSummaryDto
        {
            StudentEmail = s.StudentEmail,
            StudentName = s.StudentName,
            TotalAmount = s.TotalAmount,
            RecordCount = s.RecordCount,
            Records = s.Records.Select(r => new UserRewardRecordDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                UserId = r.UserId,
                StudentEmail = r.StudentEmail,
                StudentName = r.StudentName,
                EventId = r.EventId,
                EventTitle = r.EventTitle,
                RewardCategoryId = r.RewardCategoryId,
                RewardCategoryName = r.RewardCategoryName,
                RewardType = (RewardTypeDto)(int)r.RewardType,
                DetailName = r.DetailName,
                Amount = r.Amount,
                GrantedAt = r.GrantedAt
            }).ToList()
        }).ToList();
    }

    private RewardCategoryDto MapCategoryToDto(RewardCategory cat)
    {
        return new RewardCategoryDto
        {
            Id = cat.Id,
            TenantId = cat.TenantId,
            Name = cat.Name,
            Type = (RewardTypeDto)(int)cat.Type,
            Description = cat.Description,
            IsActive = cat.IsActive
        };
    }
}
