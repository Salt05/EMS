using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Rewards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace EMS.Mvc.Services;

public class HttpRewardService : IRewardCategoryService, IEventRewardService, IUserRewardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpRewardService> _logger;

    public HttpRewardService(HttpClient httpClient, ILogger<HttpRewardService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    #region IEventRewardService
    public async Task<List<EventReward>> GetRewardsByEventAsync(string eventId, string tenantId)
    {
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<EventRewardDto>>($"/api/rewards/events/{eventId}?tenantId={tenantId}")
                       ?? new List<EventRewardDto>();

            return dtos.Select(r => new EventReward
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
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[HttpRewardService] Failed to fetch rewards for eventId={eventId}, tenantId={tenantId}");
            return new List<EventReward>();
        }
    }

    public async Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventReward> rewards)
    {
        try
        {
            var dtos = rewards.Select(r => new EventRewardDto
            {
                Id = r.Id,
                EventId = r.EventId,
                TenantId = r.TenantId,
                RewardCategoryId = r.RewardCategoryId,
                DetailName = r.DetailName,
                ValueOrQuantity = r.ValueOrQuantity,
                Description = r.Description
            }).ToList();

            var response = await _httpClient.PostAsJsonAsync($"/api/rewards/events/{eventId}?tenantId={tenantId}", dtos);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> DeleteRewardAsync(string rewardId, string tenantId)
    {
        return Task.FromResult(true);
    }
    #endregion

    #region IRewardCategoryService
    public async Task<List<RewardCategory>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true)
    {
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<RewardCategoryDto>>($"/api/rewards/categories?tenantId={tenantId}&activeOnly={activeOnly}")
                       ?? new List<RewardCategoryDto>();

            return dtos.Select(MapCategoryToEntity).ToList();
        }
        catch
        {
            return new List<RewardCategory>();
        }
    }

    public async Task<RewardCategory?> GetCategoryByIdAsync(string id, string tenantId)
    {
        try
        {
            var dto = await _httpClient.GetFromJsonAsync<RewardCategoryDto>($"/api/rewards/categories/{id}?tenantId={tenantId}");
            return dto == null ? null : MapCategoryToEntity(dto);
        }
        catch
        {
            return null;
        }
    }

    public async Task<RewardCategory> CreateCategoryAsync(RewardCategory category)
    {
        var dto = new RewardCategoryDto
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Name = category.Name,
            Type = (RewardTypeDto)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        var response = await _httpClient.PostAsJsonAsync("/api/rewards/categories", dto);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<RewardCategoryDto>();
            if (created != null) return MapCategoryToEntity(created);
        }
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(RewardCategory category)
    {
        var dto = new RewardCategoryDto
        {
            Id = category.Id,
            TenantId = category.TenantId,
            Name = category.Name,
            Type = (RewardTypeDto)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        var response = await _httpClient.PutAsJsonAsync($"/api/rewards/categories/{category.Id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive)
    {
        var response = await _httpClient.PatchAsync($"/api/rewards/categories/{id}/toggle?isActive={isActive}&tenantId={tenantId}", null);
        return response.IsSuccessStatusCode;
    }

    private static RewardCategory MapCategoryToEntity(RewardCategoryDto dto)
    {
        return new RewardCategory
        {
            Id = dto.Id,
            TenantId = dto.TenantId,
            Name = dto.Name,
            Type = (RewardType)(int)dto.Type,
            Description = dto.Description,
            IsActive = dto.IsActive
        };
    }
    #endregion

    #region IUserRewardService
    public Task<bool> GrantRewardsOnCheckInAsync(string tenantId, string eventId, string userId, string studentEmail, string studentName)
    {
        return Task.FromResult(true);
    }

    public Task<List<UserRewardRecord>> GetUserRewardsAsync(string studentEmail, string tenantId, RewardType? type = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        return Task.FromResult(new List<UserRewardRecord>());
    }

    public async Task<List<EMS.Core.Interfaces.Services.StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(string tenantId, string? rewardCategoryId = null, RewardType? rewardType = null, DateTime? fromDate = null, DateTime? toDate = null, string sortOrder = "desc", string? searchKeyword = null)
    {
        try
        {
            var url = $"/api/rewards/stats?tenantId={tenantId}&sortOrder={sortOrder}";
            if (!string.IsNullOrEmpty(rewardCategoryId)) url += $"&rewardCategoryId={rewardCategoryId}";
            if (rewardType.HasValue) url += $"&rewardType={(int)rewardType.Value}";
            if (fromDate.HasValue) url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            if (toDate.HasValue) url += $"&toDate={toDate.Value:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(searchKeyword)) url += $"&searchKeyword={Uri.EscapeDataString(searchKeyword)}";

            var dtos = await _httpClient.GetFromJsonAsync<List<EMS.Shared.DTOs.Rewards.StudentRewardSummaryDto>>(url) ?? new List<EMS.Shared.DTOs.Rewards.StudentRewardSummaryDto>();

            return dtos.Select(s => new EMS.Core.Interfaces.Services.StudentRewardSummaryDto
            {
                StudentEmail = s.StudentEmail,
                StudentName = s.StudentName,
                TotalAmount = s.TotalAmount,
                RecordCount = s.RecordCount,
                Records = s.Records.Select(r => new UserRewardRecord
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
                    RewardType = (RewardType)(int)r.RewardType,
                    DetailName = r.DetailName,
                    Amount = r.Amount,
                    GrantedAt = r.GrantedAt
                }).ToList()
            }).ToList();
        }
        catch
        {
            return new List<EMS.Core.Interfaces.Services.StudentRewardSummaryDto>();
        }
    }
    #endregion
}
