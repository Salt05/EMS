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

    public EventsController(
        IEventService eventService, 
        IRegistrationService registrationService, 
        IAgendaService agendaService,
        ICalendarService calendarService,
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _agendaService = agendaService;
        _calendarService = calendarService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchString)
    {
        // Yêu cầu đăng nhập để xem sự kiện
        var isGuest  = HttpContext.Items["IsGuest"] as bool? ?? true;
        var tenantId = HttpContext.Items["TenantId"]?.ToString();

        if (isGuest || string.IsNullOrEmpty(tenantId))
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập bằng tài khoản sinh viên để xem sự kiện.";
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Index", "Events") });
        }

        _logger.LogInformation($"Fetching events for tenant {tenantId}");

        // Fetch only approved events for students
        var events = await _eventService.GetEventsByTenantAsync(tenantId, EventStatus.Approved);

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

        if (ev == null || ev.Status != EventStatus.Approved)
        {
            _logger.LogWarning($"Event {id} not found or not approved for tenant {tenantId}");
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện hoặc sự kiện chưa được phê duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // Get registration status if student is logged in
        var (displayName, userEmail, _) = GetUserSession();
        RegistrationStatus? regStatus = null;
        if (userEmail != null)
        {
            var studentRegs = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
            var reg = studentRegs.FirstOrDefault(r => r.EventId == id);
            if (reg != null)
            {
                regStatus = reg.Status;
                ViewBag.RegistrationCheckedIn = reg.CheckedIn;
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
        try
        {
            await _registrationService.RegisterForEventAsync(tenantId, eventId, userEmail, displayName);
            TempData["SuccessMessage"] = "Đăng ký tham gia sự kiện thành công!";
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
    public async Task<IActionResult> GenerateCheckInCode(string eventId)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện chức năng này.";
            return RedirectToAction("Login", "Auth");
        }

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
            return RedirectToAction(nameof(Index));
        }

        // Tạo mã điểm danh ngẫu nhiên gồm 6 ký tự viết hoa
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var code = new string(System.Linq.Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        ev.CheckInCode = code;
        ev.CheckInCodeExpiredAt = System.DateTime.UtcNow.AddHours(4); // Có giá trị trong 4 giờ

        var success = await _eventService.UpdateEventAsync(ev);
        if (success)
        {
            TempData["SuccessMessage"] = $"Đã tạo mã điểm danh thành công: {code}";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể cập nhật mã điểm danh cho sự kiện.";
        }

        return RedirectToAction(nameof(Detail), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> CheckIn(string id)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để check-in sự kiện.";
            return RedirectToAction("Login", "Auth");
        }

        if (string.IsNullOrEmpty(id))
            return NotFound();

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var ev = await _eventService.GetEventByIdAsync(id, tenantId);

        if (ev == null || ev.Status != EventStatus.Approved)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện hoặc sự kiện không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new CheckInViewModel { Event = ev };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(string id, string checkInCode)
    {
        var (displayName, userEmail, _) = GetUserSession();
        if (userEmail == null)
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để check-in sự kiện.";
            return RedirectToAction("Login", "Auth");
        }

        if (string.IsNullOrEmpty(id))
            return NotFound();

        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var ev = await _eventService.GetEventByIdAsync(id, tenantId);

        if (ev == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
            return RedirectToAction(nameof(Index));
        }

        var (success, message) = await _registrationService.CheckInAsync(tenantId, id, userEmail, checkInCode?.Trim()?.ToUpperInvariant() ?? "");

        if (success)
            _logger.LogInformation($"Student {userEmail} checked in to event {id}");
        else
            _logger.LogWarning($"Check-in failed for student {userEmail}, event {id}: {message}");

        var vm = new CheckInViewModel
        {
            Event = ev,
            CheckInCode = checkInCode ?? "",
            CheckInSuccess = success,
            CheckInMessage = message
        };
        return View(vm);
    }

    private (string? displayName, string? userEmail, string? userRole) GetUserSession()
    {
        string? userSession = Request.Cookies["user_session"];
        if (string.IsNullOrEmpty(userSession))
        {
            return (null, null, null);
        }

        var parts = userSession.Split('|');
        // Format mới: fullName|email|role|tenantId
        // Format cũ:  fullName|email|role
        if (parts.Length >= 3)
        {
            return (parts[0], parts[1], parts[2]);
        }

        return (null, null, null);
    }
}
