using System.Security.Claims;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly IEventService _eventService;
    private readonly IEventRewardService _eventRewardService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiService aiService, 
        IEventService eventService, 
        IEventRewardService eventRewardService,
        IRegistrationService registrationService,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _eventService = eventService;
        _eventRewardService = eventRewardService;
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// AI Magic Writer — Sinh nội dung sự kiện từ ý tưởng ngắn.
    /// Dành cho Organizer/Admin khi tạo sự kiện mới.
    /// </summary>
    [HttpPost("generate-event")]
    [Authorize(Roles = "manager,admin,superadmin")]
    public async Task<IActionResult> GenerateEventContent([FromBody] AiGenerateEventRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TopicPrompt))
            return BadRequest(new { error = "Vui lòng nhập ý tưởng sự kiện." });

        try
        {
            var result = await _aiService.GenerateEventContentAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi sinh nội dung sự kiện AI.");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý yêu cầu AI. Vui lòng thử lại." });
        }
    }

    /// <summary>
    /// AI Chatbot — Trả lời câu hỏi tư vấn sự kiện cho sinh viên.
    /// Không yêu cầu đăng nhập để tăng trải nghiệm.
    [HttpPost("chat")]
    [AllowAnonymous]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Vui lòng nhập câu hỏi." });

        try
        {
            // Lấy tenantId từ header (TenantMiddleware đã inject) hoặc từ request
            var tenantId = request.TenantId;
            if (string.IsNullOrEmpty(tenantId) || tenantId == "default")
            {
                tenantId = HttpContext.Items["TenantId"]?.ToString() ?? "all";
            }

            // Truy vấn danh sách sự kiện (Tất cả trạng thái) để cung cấp context cho AI
            var eventSummaries = new List<string>();
            try
            {
                // Bước 1: Phân tích Intent và tái tạo câu hỏi (Query Reformulation)
                var intentData = await _aiService.AnalyzeChatIntentAsync(request);

                // Lấy tất cả sự kiện bất kể trạng thái (Pending, Approved, Ongoing, Ended...)
                var events = await _eventService.GetEventsByTenantAsync(tenantId, null);
                if (!events.Any() && tenantId != "all")
                {
                    // Fallback lấy toàn bộ sự kiện hệ thống nếu tenant không có dữ liệu
                    events = await _eventService.GetEventsByTenantAsync("all", null);
                }

                // Lọc bỏ sự kiện ẩn (Hidden)
                var queryableEvents = events.Where(e => e.Scope != EMS.Core.Entities.Enums.EventScope.Hidden);

                var allEvents = queryableEvents
                    .OrderByDescending(e => e.StartTime)
                    .ToList();

                var activeEvents = new List<EMS.Core.Entities.Event>();
                var searchKeyword = intentData.SearchKeyword?.Trim();
                var userMessage = request.Message?.Trim() ?? string.Empty;

                // 1. Thử tìm khớp tên sự kiện trực tiếp trong câu nhắn người dùng hoặc từ khóa Intent
                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    activeEvents = allEvents
                        .Where(e => !string.IsNullOrWhiteSpace(e.Title) && (
                            userMessage.Contains(e.Title, StringComparison.OrdinalIgnoreCase) || 
                            e.Title.Contains(userMessage, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrWhiteSpace(searchKeyword) && e.Title.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase))
                        ))
                        .Take(5)
                        .ToList();
                }

                // 2. Nếu không thấy, thử tìm khớp từng từ đơn trong searchKeyword hoặc userMessage
                if (!activeEvents.Any())
                {
                    var targetText = string.IsNullOrWhiteSpace(searchKeyword) ? userMessage : searchKeyword;
                    var words = targetText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    
                    activeEvents = allEvents
                        .Where(e => !string.IsNullOrWhiteSpace(e.Title) && words.Any(w => w.Length >= 2 && e.Title.Contains(w, StringComparison.OrdinalIgnoreCase)))
                        .Take(5)
                        .ToList();
                }

                // 3. Nếu vẫn không thấy hoặc người dùng hỏi danh sách chung (Intent == "List")
                if (!activeEvents.Any() || intentData.Intent == "List")
                {
                    activeEvents = allEvents.Take(20).ToList();
                }

                foreach (var e in activeEvents)
                {
                    var rewards = await _eventRewardService.GetRewardsByEventAsync(e.Id, tenantId);
                    
                    // Lọc lấy các phần thưởng thực tế hợp lệ
                    var validRewards = rewards
                        .Where(r => !string.IsNullOrWhiteSpace(r.DetailName) && 
                                    !r.DetailName.Trim().Equals("Không có", StringComparison.OrdinalIgnoreCase) && 
                                    !r.DetailName.Trim().Equals("0", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    bool hasValidReward = validRewards.Any();
                    var rewardStr = hasValidReward 
                        ? string.Join(", ", validRewards.Select(r => string.IsNullOrWhiteSpace(r.Description) ? r.DetailName : $"{r.DetailName} ({r.Description})"))
                        : "Không có";

                    // Truy vấn bảng Đăng ký (Registrations) để lấy số liệu thực tế và thông tin sinh viên
                    var regs = await _registrationService.GetRegistrationsByEventAsync(e.Id, tenantId);
                    int registeredCount = regs?.Count ?? 0;
                    int approvedCount = regs?.Count(r => r.Status == EMS.Core.Entities.Enums.RegistrationStatus.Approved || r.Status == EMS.Core.Entities.Enums.RegistrationStatus.Confirmed) ?? 0;
                    int availableSlots = e.Capacity > 0 ? Math.Max(0, e.Capacity - approvedCount) : 9999;

                    var studentDetails = regs?
                        .Select(r => {
                            var name = !string.IsNullOrWhiteSpace(r.StudentName) ? r.StudentName : (!string.IsNullOrWhiteSpace(r.UserId) ? r.UserId : "Sinh viên");
                            var email = !string.IsNullOrWhiteSpace(r.StudentEmail) ? r.StudentEmail : r.UserId;
                            return string.IsNullOrWhiteSpace(email) ? name : $"{name} ({email})";
                        })
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    var studentStr = (studentDetails != null && studentDetails.Any())
                        ? string.Join(", ", studentDetails)
                        : "Chưa có sinh viên nào đăng ký";

                    eventSummaries.Add(
                        $"• Sự kiện DB: \"{e.Title}\" [ID={e.Id}] (Ảnh: {(string.IsNullOrWhiteSpace(e.ImageUrl) ? "Không có" : e.ImageUrl)}), Thời gian: {e.StartTime:dd/MM/yyyy HH:mm} - {e.EndTime:dd/MM/yyyy HH:mm} (UTC), Địa điểm: {e.Location}, Số lượng đăng ký: {registeredCount} người ({approvedCount} đã duyệt), Sức chứa: {e.Capacity} chỗ (Còn {availableSlots} chỗ trống), Danh sách sinh viên đăng ký: [{studentStr}], Giá vé: {(e.Price > 0 ? e.Price.ToString("N0") + " VND" : "Miễn phí")}, Phần thưởng: {rewardStr}, Mô tả: {(string.IsNullOrWhiteSpace(e.Description) ? "Chưa có mô tả" : e.Description)}"
                    );
                }

                // Bước 3: Nạp danh sách vé / sự kiện người dùng đã đăng ký thành công nếu có UserEmail
                if (!string.IsNullOrWhiteSpace(request.UserEmail))
                {
                    try
                    {
                        var userRegs = await _registrationService.GetRegistrationsByStudentAsync(request.UserEmail, tenantId);
                        if (userRegs != null && userRegs.Any())
                        {
                            var regEventIds = userRegs.Select(r => r.EventId).ToHashSet();
                            var regEvents = events.Where(e => regEventIds.Contains(e.Id)).ToList();
                            
                            if (regEvents.Any())
                            {
                                eventSummaries.Add("\n[THÔNG TIN DÃ ĐĂNG KÝ VÀ CÓ VÉ CỦA NGƯỜI DÙNG HIỆN TẠI (USER TICKETS)]:");
                                foreach (var re in regEvents)
                                {
                                    var reg = userRegs.First(r => r.EventId == re.Id);
                                    eventSummaries.Add($"- Người dùng ĐÃ ĐĂNG KÝ VÀ CÓ VÉ THÀNH CÔNG sự kiện: \"{re.Title}\" | ID: {re.Id} | Thời gian: {re.StartTime:dd/MM/yyyy HH:mm} - {re.EndTime:dd/MM/yyyy HH:mm} | Địa điểm: {re.Location} | Trạng thái vé: Đã xác nhận (Status: {reg.Status})");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể tải vé đã đăng ký của student {Email}", request.UserEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải danh sách sự kiện cho AI Chat context.");
            }

            var result = await _aiService.ChatWithAssistantAsync(request, eventSummaries);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý AI Chat.");
            return StatusCode(500, new { error = "Đã xảy ra lỗi. Vui lòng thử lại." });
        }
    }
}
