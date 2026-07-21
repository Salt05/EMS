using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EMS.WebAPI.Middleware;

public class ApiKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private const string APIKEYNAME = "X-API-KEY";

    public ApiKeyValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // For demonstration, you might not want to block all paths. 
        // e.g., allow swagger without API Key or login endpoint.
        var path = context.Request.Path.Value?.ToLower();
        
        // Skip validation for swagger, hangfire, Auth endpoints, and SignalR hub
        if (path != null && (path.StartsWith("/swagger") || path.StartsWith("/hangfire") || path.StartsWith("/api/auth") || path.StartsWith("/notificationhub")))
        {
            await _next(context);
            return;
        }

        // Bỏ qua check API Key nếu Request này của người dùng (đã có JWT Token trong Header hoặc Query String cho SignalR)
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) && 
            authHeader.ToString().StartsWith("Bearer "))
        {
            await _next(context);
            return;
        }

        if (context.Request.Query.ContainsKey("access_token"))
        {
            await _next(context);
            return;
        }

        var appSettingsApiKey = configuration.GetValue<string>("ApiKey");

        // If the configuration does not define an API key, we skip validation.
        if (string.IsNullOrEmpty(appSettingsApiKey))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key was not provided.");
            return;
        }

        if (!appSettingsApiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized client.");
            return;
        }

        await _next(context);
    }
}
