using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Mvc.Controllers;

/// <summary>
/// Proxy controller cho AI Chatbot trên Cổng sinh viên.
/// Chuyển tiếp tin nhắn tới IAiService (được resolve từ WebAPI HttpClient).
/// </summary>
public class AiChatController : Controller
{
    private readonly IEventService _eventService;
    private readonly ILogger<AiChatController> _logger;
    private readonly HttpClient _httpClient;

    public AiChatController(IEventService eventService, HttpClient httpClient, ILogger<AiChatController> logger)
    {
        _eventService = eventService;
        _httpClient = httpClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Vui lòng nhập câu hỏi." });

        try
        {
            // Lấy tenantId từ HttpContext (TenantMiddleware đã inject)
            var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "default";
            request.TenantId = tenantId;

            // Lấy userEmail từ cookie user_session nếu sinh viên đã đăng nhập
            string? userSession = Request.Cookies["user_session"];
            if (!string.IsNullOrEmpty(userSession))
            {
                var parts = userSession.Split('|');
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    request.UserEmail = parts[1].Trim();
                }
            }

            // Gọi thẳng WebAPI /api/ai/chat
            var response = await _httpClient.PostAsJsonAsync("/api/ai/chat", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiChatResponseDto>();
                return Json(result);
            }

            // Fallback: nếu WebAPI không khả dụng, trả lời cơ bản dựa trên sự kiện local
            return Json(await GenerateLocalFallbackAsync(request, tenantId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gọi AI Chat API. Sử dụng local fallback.");

            var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "default";
            return Json(await GenerateLocalFallbackAsync(request, tenantId));
        }
    }

    private async Task<AiChatResponseDto> GenerateLocalFallbackAsync(AiChatRequestDto request, string tenantId)
    {
        // Loại bỏ hoàn toàn việc cộng chuỗi thủ công theo yêu cầu (Tránh Hardcoded template).
        return new AiChatResponseDto 
        { 
            Reply = "⚠️ Hệ thống AI hiện đang bảo trì hoặc quá tải. Vui lòng liên hệ ban quản trị hoặc thử lại sau nhé! 😊", 
            IsFromAi = false 
        };
    }
}
