using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EMS.Mvc.Models;
using EMS.Mvc.Services;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities.Enums;

namespace EMS.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IEventService _eventService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IEventService eventService, ILogger<HomeController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = HttpContext.Items["TenantId"]?.ToString() ?? DevInMemoryTenantService.DefaultTenantId;
        var events = await _eventService.GetEventsByTenantAsync(tenantId, EventStatus.Approved);
        var topEvents = events.Take(6).ToList();
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
