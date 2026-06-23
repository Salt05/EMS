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
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService, 
        IRegistrationService registrationService, 
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchString)
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
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
