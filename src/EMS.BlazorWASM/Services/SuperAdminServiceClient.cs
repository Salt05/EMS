using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Admin;
using EMS.Shared.DTOs.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EMS.BlazorWASM.Services;

public interface ISuperAdminServiceClient
{
    Task<SuperAdminDashboardStatsDto> GetStatsAsync();
    Task<List<TenantDTO>> GetTenantsAsync(bool bypassCache = false);
    Task<TenantDTO?> CreateTenantAsync(TenantDTO tenant);
    Task<bool> UpdateTenantAsync(string id, TenantDTO tenant);
    Task<bool> DeleteTenantAsync(string id);
    Task<List<AdminUserItemDto>> GetUsersAsync(bool bypassCache = false);
    Task<bool> CreateUserAsync(AdminCreateUserRequestDto request);
    Task<bool> UpdateUserRoleAsync(string id, string roleId);
    Task<bool> UpdateUserTenantAsync(string id, string tenantId);
    Task<bool> ToggleUserActiveAsync(string id);
    Task<bool> DeleteUserAsync(string id);
    Task<List<EventResponseDto>> GetEventsAsync(bool bypassCache = false);
    Task<EventResponseDto?> GetEventByIdAsync(string id);
    Task<bool> UpdateEventAsync(string id, UpdateEventDto dto);
    Task<bool> CancelEventAsync(string id, string reason);
}

public class SuperAdminServiceClient : ISuperAdminServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SuperAdminServiceClient> _logger;

    public SuperAdminServiceClient(HttpClient httpClient, ILogger<SuperAdminServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SuperAdminDashboardStatsDto> GetStatsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<SuperAdminDashboardStatsDto>("api/superadmin/dashboard/stats");
            return res ?? new SuperAdminDashboardStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/superadmin/dashboard/stats.");
            return new SuperAdminDashboardStatsDto();
        }
    }

    public async Task<List<TenantDTO>> GetTenantsAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<TenantDTO>>($"api/superadmin/tenants?bypassCache={bypassCache}");
            return res ?? new List<TenantDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/superadmin/tenants.");
            return new List<TenantDTO>();
        }
    }

    public async Task<TenantDTO?> CreateTenantAsync(TenantDTO tenant)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/superadmin/tenants", tenant);
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<TenantDTO>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API POST /api/superadmin/tenants.");
        }

        return null;
    }

    public async Task<bool> UpdateTenantAsync(string id, TenantDTO tenant)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/tenants/{id}", tenant);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/superadmin/tenants/{id}.");
        }
        return false;
    }

    public async Task<bool> DeleteTenantAsync(string id)
    {
        try
        {
            var res = await _httpClient.DeleteAsync($"api/superadmin/tenants/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API DELETE /api/superadmin/tenants/{id}.");
        }
        return false;
    }

    public async Task<List<AdminUserItemDto>> GetUsersAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<AdminUserItemDto>>($"api/superadmin/users?bypassCache={bypassCache}");
            return res ?? new List<AdminUserItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/superadmin/users.");
            return new List<AdminUserItemDto>();
        }
    }

    public async Task<bool> CreateUserAsync(AdminCreateUserRequestDto request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/superadmin/users", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API POST /api/superadmin/users.");
        }

        return false;
    }

    public async Task<bool> UpdateUserRoleAsync(string id, string roleId)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/users/{id}/role", new AdminUpdateRoleRequestDto { RoleId = roleId });
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/superadmin/users/{id}/role.");
        }
        return false;
    }

    public async Task<bool> UpdateUserTenantAsync(string id, string tenantId)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/users/{id}/tenant", new AdminChangeTenantRequestDto { TenantId = tenantId });
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/superadmin/users/{id}/tenant.");
        }
        return false;
    }

    public async Task<bool> ToggleUserActiveAsync(string id)
    {
        try
        {
            var res = await _httpClient.PutAsync($"api/superadmin/users/{id}/toggle-active", null);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/superadmin/users/{id}/toggle-active.");
        }
        return false;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            var res = await _httpClient.DeleteAsync($"api/superadmin/users/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API DELETE /api/superadmin/users/{id}.");
        }
        return false;
    }

    public async Task<List<EventResponseDto>> GetEventsAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>($"api/superadmin/events?bypassCache={bypassCache}");
            return res ?? new List<EventResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/superadmin/events.");
            return new List<EventResponseDto>();
        }
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EventResponseDto>($"api/superadmin/events/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to fetch event {id} via API in SuperAdmin");
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(string id, UpdateEventDto dto)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/events/{id}", dto);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to update event {id} via API in SuperAdmin");
        }
        return false;
    }

    public async Task<bool> CancelEventAsync(string id, string reason)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/events/{id}/cancel", new RejectEventDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/superadmin/events/{id}/cancel.");
        }
        return false;
    }
}
