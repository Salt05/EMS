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
        // Nếu chưa đăng nhập (IsGuest = true hoặc không có TenantId) → không load events
        var isGuest  = HttpContext.Items["IsGuest"] as bool? ?? true;
        var tenantId = HttpContext.Items["TenantId"]?.ToString();

        if (isGuest || string.IsNullOrEmpty(tenantId))
        {
            // Trả về trang landing với danh sách sự kiện rỗng
            ViewBag.IsGuest = true;
            return View(new List<EMS.Core.Entities.Event>());
        }

        ViewBag.IsGuest = false;
        _logger.LogInformation($"Fetching home events for tenant {tenantId}");

        var events   = await _eventService.GetEventsByTenantAsync(tenantId, EventStatus.Approved);
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
