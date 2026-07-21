using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Exceptions;
using EMS.Mvc.Services;
using EMS.Mvc.ViewModels;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;

namespace EMS.Mvc.Controllers;

public class EventsController : Controller
{
    private readonly IEventService _eventService;
    private readonly IRegistrationService _registrationService;
    private readonly IAgendaService _agendaService;
    private readonly ICalendarService _calendarService;
    private readonly ILogger<EventsController> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public EventsController(
        IEventService eventService, 
        IRegistrationService registrationService, 
        IAgendaService agendaService,
        ICalendarService calendarService,
        ILogger<EventsController> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _agendaService = agendaService;
        _calendarService = calendarService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchString)
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        _logger.LogInformation($"Fetching events for tenant {tenantId}");

        // Fetch only approved events for students
        var events = await _eventService.GetEventsByTenantAsync(tenantId, EventStatus.Approved);

        var (_, userEmail, _) = GetUserSession();
        bool isLoggedIn = !string.IsNullOrEmpty(userEmail);

        // Filter events based on Scope
        events = events.Where(e => e.Scope != EventScope.Hidden).ToList();
        
        bool isGlobalPlatform = tenantId == "all";
        
        if (!isLoggedIn || isGlobalPlatform)
        {
            // If not logged in OR on the global platform, only show Public (0) and PublicViewTenantRegister (2)
            events = events.Where(e => e.Scope == EventScope.Public || e.Scope == EventScope.PublicViewTenantRegister).ToList();
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var searchLower = searchString.ToLower().Trim();
            events = events.Where(e => 
                e.Title.ToLower().Contains(searchLower) || 
                e.Description.ToLower().Contains(searchLower) ||
                e.Location.ToLower().Contains(searchLower)
            ).ToList();
        }

        ViewData["SearchString"] = searchString;
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var ev = await _eventService.GetEventByIdAsync(id, tenantId);

        if (ev == null || ev.Status != EventStatus.Approved || ev.Scope == EventScope.Hidden)
        {
            _logger.LogWarning($"Event {id} not found, not approved, or hidden for tenant {tenantId}");
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện hoặc sự kiện chưa được phê duyệt.";
            return RedirectToAction(nameof(Index));
        }

        var (displayName, userEmail, _) = GetUserSession();
        bool isLoggedIn = !string.IsNullOrEmpty(userEmail);
        bool isGlobalPlatform = tenantId == "all";

        if ((!isLoggedIn || isGlobalPlatform) && (ev.Scope == EventScope.Internal || ev.Scope == EventScope.TenantViewOnly))
        {
            if (isGlobalPlatform)
            {
                TempData["ErrorMessage"] = "Sự kiện này yêu cầu truy cập thông qua cổng thông tin của trường.";
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông tin sự kiện nội bộ này.";
            return RedirectToAction("Login", "Auth");
        }

        // Get registration status if student is logged in
        RegistrationStatus? regStatus = null;
        if (isLoggedIn)
        {
            var studentRegs = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
            var reg = studentRegs.FirstOrDefault(r => r.EventId == id);
            if (reg != null)
            {
                regStatus = reg.Status;
                ViewBag.RegistrationCheckedIn = reg.CheckedIn;
                ViewBag.RegistrationId = reg.Id;
            }
        }
        ViewBag.RegistrationStatus = regStatus;

        return View(ev);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string eventId)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null || displayName == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để đăng ký sự kiện.";
            return RedirectToAction("Login", "Auth");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null || ev.Scope == EventScope.Hidden)
        {
            TempData["ErrorMessage"] = "Sự kiện không tồn tại hoặc đã bị ẩn.";
            return RedirectToAction(nameof(Index));
        }

        if (ev.Scope == EventScope.TenantViewOnly)
        {
            TempData["ErrorMessage"] = "Sự kiện này chỉ dành cho xem nội bộ, không cho phép đăng ký.";
            return RedirectToAction(nameof(Detail), new { id = eventId });
        }

        bool isGlobalPlatform = tenantId == "all";
        if (isGlobalPlatform && (ev.Scope == EventScope.Internal || ev.Scope == EventScope.PublicViewTenantRegister))
        {
            TempData["ErrorMessage"] = "Sự kiện này yêu cầu tài khoản nội bộ. Vui lòng truy cập cổng thông tin của trường để đăng ký.";
            return RedirectToAction(nameof(Detail), new { id = eventId });
        }

        try
        {
            var reg = await _registrationService.RegisterForEventAsync(tenantId, eventId, userEmail, displayName);
            if (reg != null && reg.Status == RegistrationStatus.PendingPayment)
            {
                return RedirectToAction("Pay", "Payment", new { registrationId = reg.Id });
            }
            TempData["SuccessMessage"] = "Đăng ký tham gia sự kiện thành công!";

            // Trigger SignalR Notification on WebAPI
            _ = Task.Run(async () => 
            {
                try
                {
                    var webApiUrl = _configuration["WebApiBaseUrl"] ?? "https://localhost:7296";
                    var handler = new System.Net.Http.HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                    };
                    using var client = new System.Net.Http.HttpClient(handler);
                    client.DefaultRequestHeaders.Add("X-API-KEY", "Secret_EMS_Api_Key_2026");
                    var response = await client.PostAsync($"{webApiUrl}/api/notifications/trigger-registration?eventId={eventId}&tenantId={tenantId}&registrationId={reg.Id}", null);
                    _logger.LogInformation($"SignalR Trigger Response: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to trigger SignalR notification.");
                }
            });

        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering student {userEmail} for event {eventId}");
            TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi đăng ký. Vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(Detail), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string eventId, string? redirectTo = null)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện chức năng này.";
            return RedirectToAction("Login", "Auth");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        try
        {
            await _registrationService.CancelRegistrationAsync(tenantId, eventId, userEmail);
            TempData["SuccessMessage"] = "Đã hủy đăng ký sự kiện thành công.";
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling registration for student {userEmail}, event {eventId}");
            TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi hủy đăng ký. Vui lòng thử lại sau.";
        }

        if (redirectTo == "MyEvents")
        {
            return RedirectToAction(nameof(MyEvents));
        }
        return RedirectToAction(nameof(Detail), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem danh sách sự kiện của tôi.";
            return RedirectToAction("Login", "Auth");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var registrations = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);

        var registeredEvents = new List<MyEventViewModel>();
        foreach (var reg in registrations)
        {
            var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
            if (ev != null)
            {
                registeredEvents.Add(new MyEventViewModel
                {
                    Registration = reg,
                    Event = ev
                });
            }
        }

        return View(registeredEvents);
    }

    [HttpGet("api/events/{eventId}/agenda")]
    public async Task<IActionResult> GetAgenda(string eventId)
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        _logger.LogInformation($"AJAX loading agenda for event {eventId} and tenant {tenantId}");
        var agenda = await _agendaService.GetAgendaByEventAsync(eventId, tenantId);

        var response = agenda.Select(a => new
        {
            id = a.Id,
            eventId = a.EventId,
            title = a.Title,
            description = a.Description,
            speaker = a.Speaker,
            startTime = a.StartTime.ToString("dd/MM/yyyy HH:mm"),
            endTime = a.EndTime.ToString("HH:mm"),
            materialUrl = a.MaterialUrl,
            order = a.Order
        });

        return Json(response);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadIcs(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid event ID");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null)
        {
            return NotFound("Event not found");
        }

        var bytes = _calendarService.GenerateEventIcs(ev);
        return File(bytes, "text/calendar", $"{ev.Id}.ics");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInWithCode(string eventId, string code)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để check-in.";
            return RedirectToAction("Login", "Auth");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var (success, message) = await _registrationService.CheckInAsync(tenantId, eventId, userEmail, code?.Trim()?.ToUpperInvariant() ?? "");

        if (success)
        {
            TempData["SuccessMessage"] = "Điểm danh thành công! 🎉";
            TriggerCheckInNotification(eventId, tenantId);
        }
        else
        {
            TempData["ErrorMessage"] = message ?? "Mã điểm danh không hợp lệ hoặc đã hết hạn.";
        }

        return RedirectToAction(nameof(Detail), new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckInWithCodeAjax(string eventId, string checkInCode)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            return Json(new { success = false, error = "Bạn chưa đăng nhập." });
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var (success, message) = await _registrationService.CheckInAsync(tenantId, eventId, userEmail, checkInCode?.Trim()?.ToUpperInvariant() ?? "");

        if (success)
        {
            TriggerCheckInNotification(eventId, tenantId);
            var studentRegs = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
            var reg = studentRegs.FirstOrDefault(r => r.EventId == eventId);
            return Json(new { 
                success = true, 
                checkedInAt = reg?.CheckedInAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        return Json(new { success = false, error = message ?? "Mã điểm danh không hợp lệ hoặc đã hết hạn." });
    }

    private void TriggerCheckInNotification(string eventId, string tenantId)
    {
        _ = Task.Run(async () => 
        {
            try
            {
                var webApiUrl = _configuration["WebApiBaseUrl"] ?? "https://localhost:7296";
                var handler = new System.Net.Http.HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                };
                using var client = new System.Net.Http.HttpClient(handler);
                client.DefaultRequestHeaders.Add("X-API-KEY", "Secret_EMS_Api_Key_2026");
                var response = await client.PostAsync($"{webApiUrl}/api/notifications/trigger-checkin?eventId={eventId}&tenantId={tenantId}", null);
                _logger.LogInformation($"SignalR Trigger CheckIn Response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger SignalR check-in notification.");
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQrCode(string eventId)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            return Json(new { success = false, error = "Bạn cần đăng nhập." });
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var studentRegs = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
        var existingReg = studentRegs.FirstOrDefault(r => r.EventId == eventId);

        if (existingReg == null)
        {
            return Json(new { success = false, error = "Bạn chưa đăng ký tham gia sự kiện này." });
        }

        var (reg, error) = await _registrationService.GenerateCheckInCodeAsync(eventId, tenantId, existingReg.UserId);

        if (reg != null)
        {
            return Json(new
            {
                success = true,
                code = reg.CheckInCode,
                expiresAt = reg.CheckInCodeExpiresAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                registrationId = reg.Id
            });
        }

        return Json(new { success = false, error = error ?? "Không thể tạo mã QR lúc này." });
    }

    [HttpGet]
    public async Task<IActionResult> CheckInStatus(string eventId)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            return Json(new { checkedIn = false });
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var registrations = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
        var reg = registrations.FirstOrDefault(r => r.EventId == eventId);

        if (reg != null && reg.CheckedIn)
        {
            return Json(new { 
                checkedIn = true, 
                checkedInAt = reg.CheckedInAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") 
            });
        }

        return Json(new { checkedIn = false });
    }

    private (string? displayName, string? userEmail, string? userRole) GetUserSession()
    {
        string? userSession = Request.Cookies["user_session"];
        if (string.IsNullOrEmpty(userSession))
        {
            return (null, null, null);
        }

        var parts = userSession.Split('|');
        if (parts.Length >= 3)
        {
            return (parts[0], parts[1], parts[2]);
        }

        return (null, null, null);
    }
}
