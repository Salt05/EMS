using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMS.Mvc.Middlewares
{
    public class ProcessingTimeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProcessingTimeMiddleware> _logger;

        public ProcessingTimeMiddleware(RequestDelegate next, ILogger<ProcessingTimeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            context.Response.OnStarting(() =>
            {
                stopwatch.Stop();
                context.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
                _logger.LogInformation("Request {Method} {Path} took {ElapsedMilliseconds} ms", 
                    context.Request.Method, 
                    context.Request.Path, 
                    stopwatch.ElapsedMilliseconds);
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
