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

            // Check for SSO auto-login token in query parameters
            if (context.Request.Query.TryGetValue("token", out var tokenValues))
            {
                var token = tokenValues.ToString();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var parts = token.Split('.');
                        if (parts.Length == 3)
                        {
                            var payload = parts[1];
                            payload = payload.Replace('-', '+').Replace('_', '/');
                            switch (payload.Length % 4)
                            {
                                case 2: payload += "=="; break;
                                case 3: payload += "="; break;
                            }
                            var bytes = Convert.FromBase64String(payload);
                            var json = System.Text.Encoding.UTF8.GetString(bytes);
                            
                            // Parse JSON claims
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            
                            string? email = null;
                            string? fullName = null;
                            string? role = null;
                            
                            // Check for both short/long claim names
                            if (root.TryGetProperty("email", out var emailProp)) email = emailProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", out var emailProp2)) email = emailProp2.GetString();
                            
                            if (root.TryGetProperty("unique_name", out var nameProp)) fullName = nameProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", out var nameProp2)) fullName = nameProp2.GetString();
                            
                            if (root.TryGetProperty("role", out var roleProp)) role = roleProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", out var roleProp2)) role = roleProp2.GetString();
                            
                            if (!string.IsNullOrEmpty(email))
                            {
                                if (string.IsNullOrEmpty(fullName))
                                {
                                    fullName = email.Split('@')[0];
                                    if (!string.IsNullOrEmpty(fullName))
                                    {
                                        fullName = char.ToUpper(fullName[0]) + fullName.Substring(1);
                                    }
                                    else
                                    {
                                        fullName = "Sinh vien";
                                    }
                                }
                                if (string.IsNullOrEmpty(role))
                                {
                                    role = "Student";
                                }
                                
                                var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
                                {
                                    HttpOnly = true,
                                    Expires = DateTime.UtcNow.AddHours(1)
                                };
                                
                                context.Response.Cookies.Append("user_session", $"{fullName}|{email}|{role}", cookieOptions);
                                _logger.LogInformation($"SSO Auto-login successful for: {email} ({fullName}) with role: {role}");
                                
                                // Rebuild query parameters excluding "token"
                                var queryParams = new List<string>();
                                foreach (var q in context.Request.Query)
                                {
                                    if (q.Key != "token")
                                    {
                                        queryParams.Add($"{q.Key}={q.Value}");
                                    }
                                }
                                
                                var redirectUrl = context.Request.Path.Value ?? "/";
                                if (queryParams.Count > 0)
                                {
                                    redirectUrl += "?" + string.Join("&", queryParams);
                                }
                                
                                context.Response.Redirect(redirectUrl);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"SSO Auto-login failed: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant in TenantMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}
