using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EMS.Core.Interfaces.Services;

namespace EMS.Infrastructure.Services;

public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantResolver> _logger;

    public TenantResolver(IHttpContextAccessor httpContextAccessor, ILogger<TenantResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? ResolveTenantFromHost(string host)
    {
        try
        {
            // host format: "subdomain.ems.com" or "subdomain.localhost:5000"
            var parts = host.Split('.');

            if (parts.Length < 2)
            {
                _logger.LogWarning($"Invalid host format: {host}");
                return null;
            }

            var subdomain = parts[0];

            // Skip if it's localhost or main domain
            if (subdomain == "localhost" || subdomain == "ems")
            {
                return null;
            }

            _logger.LogInformation($"Resolved tenant subdomain: {subdomain} from host: {host}");
            return subdomain;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant from host: {ex.Message}");
            return null;
        }
    }

    public string? ResolveTenantIdFromContext()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Try to get from HttpContext Items (set by middleware)
            if (httpContext.Items.TryGetValue("TenantId", out var tenantId))
            {
                return tenantId?.ToString();
            }

            // Try to get from query string
            if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId))
            {
                return queryTenantId.ToString();
            }

            // Try to get from headers
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
            {
                return headerTenantId.ToString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant from context: {ex.Message}");
            return null;
        }
    }
}
