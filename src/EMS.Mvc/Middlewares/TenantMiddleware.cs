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
            // ── 1. Ưu tiên đọc tenantId từ cookie user_session ──────────────────
            //       Format: fullName|email|role|tenantId
            var tenantIdFromSession = TryGetTenantIdFromCookie(context);

            if (!string.IsNullOrEmpty(tenantIdFromSession))
            {
                var sessionTenant = await tenantService.GetTenantByIdAsync(tenantIdFromSession);
                if (sessionTenant != null)
                {
                    context.Items["TenantId"]   = sessionTenant.Id;
                    context.Items["TenantName"] = sessionTenant.Name;
                    context.Items["IsGuest"]    = false;
                    _logger.LogInformation($"Tenant from session: {sessionTenant.Name} ({sessionTenant.Id})");
                    await _next(context);
                    return;
                }
            }

            // ── 2. Fallback: resolve từ subdomain (giống logic cũ) ─────────────
            var host      = context.Request.Host.Host;
            var subdomain = tenantResolver.ResolveTenantFromHost(host);

            if (string.IsNullOrEmpty(subdomain))
                subdomain = "default";

            var tenant = await tenantService.GetTenantBySubdomainAsync(subdomain);

            // Nếu vẫn không tìm thấy, không gán tenantId (user chưa đăng nhập)
            if (tenant == null)
            {
                context.Items["TenantId"]   = null;
                context.Items["TenantName"] = "EMS Portal";
                context.Items["IsGuest"]    = true;
                _logger.LogInformation($"No tenant resolved for subdomain '{subdomain}' — treating as guest.");
            }
            else
            {
                context.Items["TenantId"]   = tenant.Id;
                context.Items["TenantName"] = tenant.Name;
                // Nếu không có session cookie → guest (không xem được events)
                context.Items["IsGuest"]    = true;
                _logger.LogInformation($"Tenant from subdomain: {tenant.Name} ({tenant.Id})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant in TenantMiddleware: {ex.Message}");
            context.Items["IsGuest"] = true;
        }

        await _next(context);
    }

    private static string? TryGetTenantIdFromCookie(HttpContext context)
    {
        var session = context.Request.Cookies["user_session"];
        if (string.IsNullOrEmpty(session)) return null;

        var parts = session.Split('|');
        // Format mới: fullName|email|role|tenantId (4 phần)
        if (parts.Length >= 4)
            return parts[3];

        return null;
    }
}
