using System.Security.Claims;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;

namespace EMS.WebAPI.Middleware;

/// <summary>
/// Middleware kiểm tra trạng thái user trên mỗi request đã xác thực.
/// Nếu user đã bị Inactive/Suspended thì trả 401 ngay lập tức,
/// dù token JWT vẫn còn hạn.
/// </summary>
public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserStatusMiddleware> _logger;

    public UserStatusMiddleware(RequestDelegate next, ILogger<UserStatusMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        // Chỉ kiểm tra nếu user đã xác thực
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = context.User.FindFirst("tenantId")?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tenantId))
            {
                var user = await userService.GetUserByIdAsync(userId, tenantId);

                if (user != null && user.Status != UserStatus.Active)
                {
                    _logger.LogWarning("Request blocked for inactive user {UserId} (Status: {Status})", userId, user.Status);

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.\"}");
                    return; // Short-circuit — không cho đi tiếp
                }
            }
        }

        await _next(context);
    }
}
