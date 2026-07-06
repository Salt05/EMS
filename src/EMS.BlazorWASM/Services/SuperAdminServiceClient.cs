using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Admin;
using EMS.Shared.DTOs.Events;
using EMS.BlazorWASM.MockData;
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
            return res ?? DashboardStatsMock.SuperAdminStats;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/superadmin/dashboard/stats chưa có, đang dùng mock data.");
            return DashboardStatsMock.SuperAdminStats;
        }
    }

    public async Task<List<TenantDTO>> GetTenantsAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<TenantDTO>>($"api/superadmin/tenants?bypassCache={bypassCache}");
            return res ?? TenantsMock.Tenants;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/superadmin/tenants chưa có, đang dùng mock data.");
            return TenantsMock.Tenants;
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
        catch
        {
            _logger.LogWarning("[DEMO] API POST /api/superadmin/tenants chưa có, đang thêm vào mock data.");
        }
        
        tenant.Id = Guid.NewGuid().ToString().Substring(0, 8);
        tenant.CreatedAt = DateTime.UtcNow;
        TenantsMock.Tenants.Add(tenant);
        return tenant;
    }

    public async Task<bool> UpdateTenantAsync(string id, TenantDTO tenant)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/tenants/{id}", tenant);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/superadmin/tenants/{id} chưa có, đang cập nhật mock data.");
        }

        var idx = TenantsMock.Tenants.FindIndex(t => t.Id == id);
        if (idx >= 0)
        {
            TenantsMock.Tenants[idx] = tenant;
            return true;
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
        catch
        {
            _logger.LogWarning($"[DEMO] API DELETE /api/superadmin/tenants/{id} chưa có, đang xóa trên mock data.");
        }

        var tenant = TenantsMock.Tenants.Find(t => t.Id == id);
        if (tenant != null)
        {
            TenantsMock.Tenants.Remove(tenant);
            return true;
        }
        return false;
    }

    public async Task<List<AdminUserItemDto>> GetUsersAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<AdminUserItemDto>>($"api/superadmin/users?bypassCache={bypassCache}");
            return res ?? UsersMock.Users;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/superadmin/users chưa có, đang dùng mock data.");
            return UsersMock.Users;
        }
    }

    public async Task<bool> CreateUserAsync(AdminCreateUserRequestDto request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/superadmin/users", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API POST /api/superadmin/users chưa có, đang thêm vào mock data.");
        }

        var user = new AdminUserItemDto
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            MSSV = request.MSSV,
            FullName = request.FullName + " (DEMO)",
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Department = request.Department,
            RoleIds = new List<string> { request.RoleId },
            TenantId = request.TenantId ?? "huflit",
            TenantName = request.TenantId == "huflit" ? "ĐH HUFLIT" : "ĐH Bách Khoa",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow
        };
        UsersMock.Users.Add(user);
        return true;
    }

    public async Task<bool> UpdateUserRoleAsync(string id, string roleId)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/users/{id}/role", new AdminUpdateRoleRequestDto { RoleId = roleId });
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/superadmin/users/{id}/role chưa có, đang cập nhật mock data.");
        }

        var user = UsersMock.Users.Find(u => u.Id == id);
        if (user != null)
        {
            user.RoleIds = new List<string> { roleId };
            return true;
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
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/superadmin/users/{id}/tenant chưa có, đang cập nhật mock data.");
        }

        var user = UsersMock.Users.Find(u => u.Id == id);
        if (user != null)
        {
            user.TenantId = tenantId;
            user.TenantName = tenantId == "huflit" ? "ĐH HUFLIT" : "ĐH Bách Khoa";
            return true;
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
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/superadmin/users/{id}/toggle-active chưa có, đang cập nhật mock data.");
        }

        var user = UsersMock.Users.Find(u => u.Id == id);
        if (user != null)
        {
            user.Status = user.Status == 1 ? 2 : 1;
            user.StatusName = user.Status == 1 ? "Active" : "Inactive";
            return true;
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
        catch
        {
            _logger.LogWarning($"[DEMO] API DELETE /api/superadmin/users/{id} chưa có, đang xóa trên mock data.");
        }

        var user = UsersMock.Users.Find(u => u.Id == id);
        if (user != null)
        {
            UsersMock.Users.Remove(user);
            return true;
        }
        return false;
    }

    public async Task<List<EventResponseDto>> GetEventsAsync(bool bypassCache = false)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>($"api/superadmin/events?bypassCache={bypassCache}");
            return res ?? EventsMock.Events;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/superadmin/events chưa có, đang dùng mock data.");
            return EventsMock.Events;
        }
    }

    public async Task<bool> CancelEventAsync(string id, string reason)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/superadmin/events/{id}/cancel", new RejectEventDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/superadmin/events/{id}/cancel chưa có, đang hủy trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            ev.Status = 5; // Cancelled
            ev.StatusName = "Cancelled";
            ev.RejectionReason = reason;
            return true;
        }
        return false;
    }
}
