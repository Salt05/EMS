using EMS.Core.Interfaces.Services;
using EMS.Mvc.Services;

namespace EMS.Mvc.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantService tenantService)
    {
        try
        {
            var host = context.Request.Host.Host;
            var subdomain = tenantResolver.ResolveTenantFromHost(host);

            if (string.IsNullOrEmpty(subdomain))
            {
                subdomain = "default";
            }

            var tenant = await tenantService.GetTenantBySubdomainAsync(subdomain);

            // Fallback: If tenant is not found (common on localhost without custom subdomain),
            // fetch all tenants and pick the first one, or fallback to tenant-1.
            if (tenant == null)
            {
                var tenants = await tenantService.GetTenantsAsync();
                if (tenants.Count > 0)
                {
                    tenant = tenants[0];
                }
            }

            if (tenant != null)
            {
                context.Items["TenantId"] = tenant.Id;
                context.Items["TenantName"] = tenant.Name;
                _logger.LogInformation($"Tenant resolved: {tenant.Name} ({tenant.Id}) for subdomain: {subdomain}");
            }
            else
            {
                context.Items["TenantId"] = DevInMemoryTenantService.DefaultTenantId;
                context.Items["TenantName"] = "EMS Portal";
                _logger.LogWarning($"Could not resolve tenant for subdomain: {subdomain}. Falling back to default.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant in TenantMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}
