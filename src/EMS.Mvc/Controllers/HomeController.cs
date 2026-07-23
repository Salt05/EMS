using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using EMS.Mvc.Models;
using EMS.Mvc.Services;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities.Enums;
using EMS.Core.Entities;

namespace EMS.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IEventService _eventService;
    private readonly IRegistrationService _registrationService;
    private readonly IAgendaService _agendaService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IEventService eventService, 
        IRegistrationService registrationService, 
        IAgendaService agendaService,
        ILogger<HomeController> logger)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _agendaService = agendaService;
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

        // Check registered events for logged in student
        if (!isGuest)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                var studentRegs = await _registrationService.GetRegistrationsByStudentAsync(userEmail, tenantId);
                var activeRegs = studentRegs
                    .Where(r => r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.Pending || r.Status == RegistrationStatus.PendingPayment)
                    .ToList();

                if (activeRegs.Any())
                {
                    var now = DateTime.UtcNow;
                    var registeredEvents = new List<(Registration Reg, Event Ev)>();

                    foreach (var reg in activeRegs)
                    {
                        var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
                        if (ev != null && ev.Status != EventStatus.Cancelled && ev.Status != EventStatus.Rejected)
                        {
                            registeredEvents.Add((reg, ev));
                        }
                    }

                    // 1. Check for ONGOING Event
                    var ongoing = registeredEvents.FirstOrDefault(x => x.Ev.StartTime <= now && now <= x.Ev.EndTime);
                    if (ongoing.Ev != null)
                    {
                        var agendaList = await _agendaService.GetAgendaByEventAsync(ongoing.Ev.Id, tenantId);
                        var sortedAgenda = agendaList.OrderBy(a => a.StartTime).ToList();

                        var currentItem = sortedAgenda.FirstOrDefault(a => a.StartTime <= now && now <= a.EndTime);
                        AgendaItem? nextItem = null;

                        if (currentItem != null)
                        {
                            var idx = sortedAgenda.IndexOf(currentItem);
                            if (idx >= 0 && idx < sortedAgenda.Count - 1)
                            {
                                nextItem = sortedAgenda[idx + 1];
                            }
                        }
                        else
                        {
                            nextItem = sortedAgenda.FirstOrDefault(a => a.StartTime > now);
                        }

                        ViewBag.UpcomingBanner = new UpcomingEventBannerViewModel
                        {
                            Event = ongoing.Ev,
                            Registration = ongoing.Reg,
                            IsOngoing = true,
                            AgendaItems = sortedAgenda,
                            CurrentAgendaItem = currentItem,
                            NextAgendaItem = nextItem
                        };
                    }
                    else
                    {
                        // 2. Check for UPCOMING Event starting within 24 Hours
                        var upcomingWithin24h = registeredEvents
                            .Where(x => x.Ev.StartTime > now && (x.Ev.StartTime - now).TotalHours <= 24)
                            .OrderBy(x => x.Ev.StartTime)
                            .FirstOrDefault();

                        if (upcomingWithin24h.Ev != null)
                        {
                            ViewBag.UpcomingBanner = new UpcomingEventBannerViewModel
                            {
                                Event = upcomingWithin24h.Ev,
                                Registration = upcomingWithin24h.Reg,
                                IsUpcomingWithin24h = true,
                                TimeUntilStart = upcomingWithin24h.Ev.StartTime - now
                            };
                        }
                    }
                }
            }
        }

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
