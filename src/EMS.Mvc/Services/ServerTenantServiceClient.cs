using EMS.BlazorWASM.Services;
using EMS.Shared.DTOs;

namespace EMS.Mvc.Services;

public class ServerTenantServiceClient : ITenantServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerTenantServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<TenantDTO>> GetTenantsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TenantDTO>>("/api/tenants") ?? new List<TenantDTO>();
        }
        catch
        {
            return new List<TenantDTO>();
        }
    }

    public Task SetCurrentTenantAsync(string tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["TenantId"] = tenantId;
        }
        return Task.CompletedTask;
    }

    public Task<string?> GetCurrentTenantIdAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var tenantId = httpContext?.Items["TenantId"]?.ToString();
        return Task.FromResult(tenantId);
    }
}
