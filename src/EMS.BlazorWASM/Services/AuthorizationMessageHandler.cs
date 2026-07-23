using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// DelegatingHandler tự động gắn JWT Bearer token và X-Tenant-ID header
/// vào mọi HTTP request gửi tới WebAPI.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public AuthorizationMessageHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Intercept HTTP request để thêm Authorization header và X-Tenant-ID.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Attach JWT token
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                token = token.Trim('"');
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Attach ApiKey header
            if (!request.Headers.Contains("X-API-KEY"))
            {
                request.Headers.Add("X-API-KEY", "Secret_EMS_Api_Key_2026");
            }

            // Attach Tenant ID
            var tenantId = await _localStorage.GetItemAsStringAsync("currentTenantId");
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                tenantId = tenantId.Trim('"');
                request.Headers.Add("X-Tenant-ID", tenantId);
            }
        }
        catch
        {
            // If localStorage is not available (e.g., pre-rendering), continue without headers
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
