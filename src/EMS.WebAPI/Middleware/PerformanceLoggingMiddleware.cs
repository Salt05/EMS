using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMS.WebAPI.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Performance Warning: The request {Method} {Path} took {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Performance: The request {Method} {Path} took {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMilliseconds);
        }
    }
}
