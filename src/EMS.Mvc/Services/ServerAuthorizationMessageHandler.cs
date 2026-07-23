using System.Net.Http.Headers;

namespace EMS.Mvc.Services;

public class ServerAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerAuthorizationMessageHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var tokenClaim = httpContext.User.FindFirst("jwt_token");
                if (tokenClaim != null && !string.IsNullOrWhiteSpace(tokenClaim.Value))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenClaim.Value);
                }

                var tenantId = httpContext.Items["TenantId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    request.Headers.TryAddWithoutValidation("X-Tenant-ID", tenantId);
                }
            }

            if (!request.Headers.Contains("X-API-KEY"))
            {
                request.Headers.TryAddWithoutValidation("X-API-KEY", "Secret_EMS_ApiKey_2026");
            }
        }
        catch
        {
            // Fallback for pre-rendering
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
