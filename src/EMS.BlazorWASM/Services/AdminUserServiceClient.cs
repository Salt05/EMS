using System.Net.Http.Json;
using EMS.Shared.DTOs.Admin;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Client-side service gọi WebAPI admin user endpoints.
/// Theo pattern của EventServiceClient.
/// </summary>
public class AdminUserServiceClient : IAdminUserServiceClient
{
    private readonly HttpClient _httpClient;

    public AdminUserServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AdminUserListResponseDto> GetUsersAsync(AdminUserFilterDto filter)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={filter.Page}",
                $"pageSize={filter.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(filter.Search))
                queryParams.Add($"search={Uri.EscapeDataString(filter.Search)}");
            if (!string.IsNullOrWhiteSpace(filter.RoleId))
                queryParams.Add($"roleId={Uri.EscapeDataString(filter.RoleId)}");
            if (!string.IsNullOrWhiteSpace(filter.TenantId))
                queryParams.Add($"tenantId={Uri.EscapeDataString(filter.TenantId)}");
            if (filter.Status.HasValue)
                queryParams.Add($"status={filter.Status.Value}");

            var url = $"/api/admin/users?{string.Join("&", queryParams)}";
            var result = await _httpClient.GetFromJsonAsync<AdminUserListResponseDto>(url);
            return result ?? new AdminUserListResponseDto();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching admin users: {ex.Message}");
            return new AdminUserListResponseDto();
        }
    }

    public async Task<AdminUserItemDto?> GetUserByIdAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdminUserItemDto>($"/api/admin/users/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(AdminCreateUserRequestDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/admin/users", dto);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(string id, AdminUpdateUserRequestDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/users/{id}", dto);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(string id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/admin/users/{id}");
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> ChangeRoleAsync(string id, AdminUpdateRoleRequestDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/users/{id}/role", dto);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> ToggleActiveAsync(string id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/admin/users/{id}/toggle-active", null);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> ChangeTenantAsync(string id, AdminChangeTenantRequestDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/admin/users/{id}/tenant", dto);
            if (response.IsSuccessStatusCode) return (true, null);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
