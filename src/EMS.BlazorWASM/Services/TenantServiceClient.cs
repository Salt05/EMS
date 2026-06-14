using System.Net.Http.Json;
using Blazored.LocalStorage;
using EMS.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Client-side tenant service cho Blazor WASM.
/// Gọi WebAPI để lấy danh sách tenants, quản lý tenant hiện tại trong localStorage.
/// </summary>
public class TenantServiceClient : ITenantServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<TenantServiceClient> _logger;

    private const string TenantKey = "currentTenantId";

    public TenantServiceClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        ILogger<TenantServiceClient> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tất cả tenants từ WebAPI.
    /// </summary>
    public async Task<List<TenantDTO>> GetTenantsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<TenantDTO>>("api/tenants");
            return response ?? new List<TenantDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tenants");
            return new List<TenantDTO>();
        }
    }

    /// <summary>
    /// Đặt tenant hiện tại, lưu vào localStorage.
    /// </summary>
    public async Task SetCurrentTenantAsync(string tenantId)
    {
        try
        {
            await _localStorage.SetItemAsStringAsync(TenantKey, tenantId);
            _logger.LogInformation("Switched to tenant: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set current tenant: {TenantId}", tenantId);
        }
    }

    /// <summary>
    /// Lấy ID tenant hiện tại từ localStorage.
    /// </summary>
    public async Task<string?> GetCurrentTenantIdAsync()
    {
        try
        {
            var tenantId = await _localStorage.GetItemAsStringAsync(TenantKey);
            return string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim('"');
        }
        catch
        {
            return null;
        }
    }
}
