using EMS.Shared.DTOs.Rewards;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EMS.BlazorWASM.Services;

public class RewardServiceClient : IRewardServiceClient
{
    private readonly HttpClient _httpClient;

    public RewardServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RewardCategoryDto>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RewardCategoryDto>>($"/api/rewards/categories?tenantId={tenantId}&activeOnly={activeOnly}") ?? new List<RewardCategoryDto>();
        }
        catch
        {
            return new List<RewardCategoryDto>();
        }
    }

    public async Task<RewardCategoryDto?> GetCategoryByIdAsync(string id, string tenantId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<RewardCategoryDto>($"/api/rewards/categories/{id}?tenantId={tenantId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<RewardCategoryDto> CreateCategoryAsync(RewardCategoryDto category)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/rewards/categories", category);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RewardCategoryDto>() ?? category;
        }
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(RewardCategoryDto category)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/rewards/categories/{category.Id}", category);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive)
    {
        var response = await _httpClient.PatchAsync($"/api/rewards/categories/{id}/toggle?isActive={isActive}&tenantId={tenantId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<EventRewardDto>> GetRewardsByEventAsync(string eventId, string tenantId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<EventRewardDto>>($"/api/rewards/events/{eventId}?tenantId={tenantId}") ?? new List<EventRewardDto>();
        }
        catch
        {
            return new List<EventRewardDto>();
        }
    }

    public async Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventRewardDto> rewards)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/rewards/events/{eventId}?tenantId={tenantId}", rewards);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(
        string tenantId,
        string? rewardCategoryId = null,
        RewardTypeDto? rewardType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortOrder = "desc",
        string? searchKeyword = null)
    {
        try
        {
            var url = $"/api/rewards/stats?tenantId={tenantId}&sortOrder={sortOrder}";
            if (!string.IsNullOrEmpty(rewardCategoryId)) url += $"&rewardCategoryId={rewardCategoryId}";
            if (rewardType.HasValue) url += $"&rewardType={(int)rewardType.Value}";
            if (fromDate.HasValue) url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            if (toDate.HasValue) url += $"&toDate={toDate.Value:yyyy-MM-dd}";
            if (!string.IsNullOrWhiteSpace(searchKeyword)) url += $"&searchKeyword={Uri.EscapeDataString(searchKeyword)}";

            return await _httpClient.GetFromJsonAsync<List<StudentRewardSummaryDto>>(url) ?? new List<StudentRewardSummaryDto>();
        }
        catch
        {
            return new List<StudentRewardSummaryDto>();
        }
    }
}
