using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using EMS.WebAPI.Hubs;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IRegistrationService _registrationService;
    private readonly IEventService _eventService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IHubContext<NotificationHub> hubContext, IRegistrationService registrationService, IEventService eventService, ILogger<NotificationsController> logger)
    {
        _hubContext = hubContext;
        _registrationService = registrationService;
        _eventService = eventService;
        _logger = logger;
    }

    [HttpPost("trigger-registration")]
    [AllowAnonymous] 
    public async Task<IActionResult> TriggerRegistrationNotification([FromQuery] string eventId, [FromQuery] string tenantId, [FromQuery] string registrationId = "")
    {
        _logger.LogInformation($"[SignalR] Triggering notification for EventId: {eventId}, TenantId: {tenantId}, RegId: {registrationId}");
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev != null)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(eventId, tenantId);
            var activeCount = regs.Count(r => r.Status == RegistrationStatus.Pending || r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.PendingPayment);
            var remainingSlots = ev.Capacity > 0 ? Math.Max(0, ev.Capacity - activeCount) : -1;
            _logger.LogInformation($"[SignalR] Broadcasting capacity {remainingSlots} to student group.");
            await _hubContext.Clients.Group("student").SendAsync("ReceiveEventCapacityUpdate", ev.Id, remainingSlots);
            
            _logger.LogInformation($"[SignalR] Broadcasting to admin_{tenantId}, manager_{tenantId} and admin groups.");
            
            await _hubContext.Clients.Groups(new[] { $"admin_{tenantId}", $"manager_{tenantId}", "admin" })
                .SendAsync("ReceiveNewRegistrationInfo", ev.Title, 1, registrationId);
            
            if (!string.IsNullOrEmpty(ev.OrganizerId))
            {
                await _hubContext.Clients.User(ev.OrganizerId).SendAsync("ReceiveNewRegistrationInfo", ev.Title, 1, registrationId);
            }
            return Ok(new { success = true });
        }
        
        _logger.LogWarning($"[SignalR] Event not found: {eventId} in tenant: {tenantId}");
        return NotFound(new { success = false, message = "Event not found" });
    }
    [HttpPost("trigger-checkin")]
    [AllowAnonymous]
    public async Task<IActionResult> TriggerCheckInNotification([FromQuery] string eventId, [FromQuery] string tenantId)
    {
        _logger.LogInformation($"[SignalR] Triggering check-in notification for EventId: {eventId}, TenantId: {tenantId}");
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev != null)
        {
            await _hubContext.Clients.Groups(new[] { $"admin_{tenantId}", $"manager_{tenantId}", "admin" })
                .SendAsync("ReceiveNewCheckInInfo", eventId);
            
            if (!string.IsNullOrEmpty(ev.OrganizerId))
            {
                await _hubContext.Clients.User(ev.OrganizerId).SendAsync("ReceiveNewCheckInInfo", eventId);
            }
            return Ok(new { success = true });
        }
        
        return NotFound(new { success = false, message = "Event not found" });
    }
}
