using EMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Mvc.Components;

public class UpcomingEventsViewComponent : ViewComponent
{
    private readonly IEventService _eventService;
    private readonly ITenantResolver _tenantResolver;

    public UpcomingEventsViewComponent(IEventService eventService, ITenantResolver tenantResolver)
    {
        _eventService = eventService;
        _tenantResolver = tenantResolver;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tenantId = _tenantResolver.ResolveTenantIdFromContext();
        
        if (string.IsNullOrEmpty(tenantId))
        {
            return View(new List<EMS.Core.Entities.Event>());
        }

        var events = await _eventService.GetEventsByTenantAsync(tenantId);
        
        bool isGuest = !(User.Identity?.IsAuthenticated ?? false);
        if (isGuest)
        {
            events = events.Where(e => (int)e.Scope == 0 || (int)e.Scope == 2).ToList();
        }

        var upcomingEvents = events
            .Where(e => e.StartTime > System.DateTime.UtcNow && e.Status == EMS.Core.Entities.Enums.EventStatus.Approved)
            .OrderBy(e => e.StartTime)
            .Take(5)
            .ToList();

        return View(upcomingEvents);
    }
}
