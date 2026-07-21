using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using EMS.Mvc.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

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

            Tenant? tenant = null;

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var authenticatedTenantId = context.User.FindFirstValue("tenantId");
                if (!string.IsNullOrEmpty(authenticatedTenantId))
                {
                    tenant = await tenantService.GetTenantByIdAsync(authenticatedTenantId);
                }
            }

            // Check for SSO auto-login token in query parameters first so we can
            // prefer the tenant encoded in the JWT over the localhost fallback.
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
                            string? userId = null;
                            string? tenantId = null;
                            
                            // Check for both short/long claim names
                            if (root.TryGetProperty("email", out var emailProp)) email = emailProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", out var emailProp2)) email = emailProp2.GetString();
                            
                            if (root.TryGetProperty("unique_name", out var nameProp)) fullName = nameProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", out var nameProp2)) fullName = nameProp2.GetString();

                            if (root.TryGetProperty("sub", out var subProp)) userId = subProp.GetString();
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var subProp2)) userId = subProp2.GetString();

                            if (root.TryGetProperty("tenantId", out var tenantIdProp))
                            {
                                tenantId = tenantIdProp.GetString();
                            }

                            if (!string.IsNullOrEmpty(tenantId))
                            {
                                tenant = await tenantService.GetTenantByIdAsync(tenantId);
                            }
                            
                            if (root.TryGetProperty("role", out var roleProp))
                            {
                                if (roleProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var roles = new List<string>();
                                    foreach (var r in roleProp.EnumerateArray())
                                    {
                                        var val = r.GetString();
                                        if (val != null) roles.Add(val);
                                    }
                                    role = roles.FirstOrDefault(x => x.Equals("admin", StringComparison.OrdinalIgnoreCase) || x.Equals("superadmin", StringComparison.OrdinalIgnoreCase) || x.Equals("manager", StringComparison.OrdinalIgnoreCase)) ?? roles.FirstOrDefault() ?? "Student";
                                }
                                else
                                {
                                    role = roleProp.GetString();
                                }
                            }
                            else if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", out var roleProp2))
                            {
                                if (roleProp2.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    var roles = new List<string>();
                                    foreach (var r in roleProp2.EnumerateArray())
                                    {
                                        var val = r.GetString();
                                        if (val != null) roles.Add(val);
                                    }
                                    role = roles.FirstOrDefault(x => x.Equals("admin", StringComparison.OrdinalIgnoreCase) || x.Equals("superadmin", StringComparison.OrdinalIgnoreCase) || x.Equals("manager", StringComparison.OrdinalIgnoreCase)) ?? roles.FirstOrDefault() ?? "Student";
                                }
                                else
                                {
                                    role = roleProp2.GetString();
                                }
                            }

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
                                
                                // Sign in using ASP.NET Core Cookie Authentication
                                var claims = new List<System.Security.Claims.Claim>
                                {
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, fullName),
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email),
                                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role),
                                    new System.Security.Claims.Claim("role", role),
                                    new System.Security.Claims.Claim("jwt_token", token)
                                };

                                if (!string.IsNullOrEmpty(userId))
                                {
                                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId));
                                    claims.Add(new System.Security.Claims.Claim("sub", userId));
                                }

                                if (tenant != null)
                                {
                                    claims.Add(new System.Security.Claims.Claim("tenantId", tenant.Id));
                                }

                                var identity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                                var principal = new System.Security.Claims.ClaimsPrincipal(identity);

                                // Make the authenticated principal visible to the current request immediately.
                                context.User = principal;
                                
                                await context.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, principal);

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

            if (tenant == null)
            {
                if (!string.IsNullOrEmpty(subdomain))
                {
                    tenant = await tenantService.GetTenantBySubdomainAsync(subdomain);
                }

                // DO NOT fallback to the first tenant. Let it be the generic EMS platform.
            }

            if (tenant != null)
            {
                context.Items["TenantId"] = tenant.Id;
                context.Items["TenantName"] = tenant.Name;
                _logger.LogInformation($"Tenant resolved: {tenant.Name} ({tenant.Id}) for subdomain: {subdomain}");
            }
            else
            {
                context.Items["TenantId"] = "all";
                context.Items["TenantName"] = "EMS";
                _logger.LogWarning($"Could not resolve tenant for subdomain: {subdomain}. Falling back to global platform.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error resolving tenant in TenantMiddleware: {ex.Message}");
        }

        await _next(context);
    }
}
