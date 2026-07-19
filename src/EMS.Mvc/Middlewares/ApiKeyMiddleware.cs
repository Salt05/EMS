using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EMS.Mvc.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private const string APIKEYNAME = "X-API-KEY";

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> _logger)
        {
            _next = next;
            this._logger = _logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // We only enforce API Key authentication for paths starting with "/api/internal/"
            if (context.Request.Path.StartsWithSegments("/api/internal"))
            {
                if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
                {
                    _logger.LogWarning("API Key was not provided for internal API access.");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("API Key was not provided.");
                    return;
                }

                var appSettings = context.RequestServices.GetRequiredService<IConfiguration>();
                var apiKey = appSettings.GetValue<string>("Security:ApiKey") ?? "EMS-Secret-API-Key-2026";

                if (!apiKey.Equals(extractedApiKey))
                {
                    _logger.LogWarning("Unauthorized client access attempt with invalid API Key: {ExtractedKey}", extractedApiKey.ToString());
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized client.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
