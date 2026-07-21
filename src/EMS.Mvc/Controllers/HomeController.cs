using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using EMS.Mvc.Models;
using EMS.Mvc.Services;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities.Enums;

namespace EMS.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IEventService _eventService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IEventService eventService, IRegistrationService registrationService, ILogger<HomeController> logger)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        bool isGuest = !(User.Identity?.IsAuthenticated ?? false);
        ViewBag.IsGuest = isGuest;
        ViewBag.DisplayName = User.FindFirstValue(ClaimTypes.Name);
        var events = await _eventService.GetEventsByTenantAsync(tenantId, EventStatus.Approved);
        
        if (isGuest)
        {
            events = events.Where(e => (int)e.Scope == 0 || (int)e.Scope == 2).ToList();
        }
        
        var topEvents = events.Take(6).ToList();

        var remainingSlotsDict = new Dictionary<string, int>();
        foreach (var ev in topEvents)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var activeCount = regs.Count(r => r.Status == RegistrationStatus.Pending || r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.PendingPayment);
            remainingSlotsDict[ev.Id] = ev.Capacity > 0 ? Math.Max(0, ev.Capacity - activeCount) : -1;
        }
        ViewBag.RemainingSlots = remainingSlotsDict;

        return View(topEvents);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
