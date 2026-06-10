using EMS.Core.Interfaces.Services;

namespace EMS.WebAPI.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        try
        {
            var host = context.Request.Host.Host;
            var subdomain = tenantResolver.ResolveTenantFromHost(host);

            if (!string.IsNullOrEmpty(subdomain))
            {
                context.Items["Subdomain"] = subdomain;
                _logger.LogInformation($"Tenant subdomain resolved: {subdomain}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in TenantMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}
