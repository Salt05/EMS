using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ICalendarService _calendarService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventService eventService,
        ICalendarService calendarService,
        ITenantService tenantService,
        ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _calendarService = calendarService;
        _tenantService = tenantService;
        _logger = logger;
    }

    // GET /api/events?status=1
    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] EventStatus? status = null)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var events = await _eventService.GetEventsByTenantAsync(tenantId, status);
        return Ok(events.Select(MapToResponse));
    }

    // GET /api/events/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        return Ok(MapToResponse(ev));
    }

    // POST /api/events
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime must be after StartTime");

        var ev = new Event
        {
            TenantId = tenantId,
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            VenueId = dto.VenueId,
            StartTime = dto.StartTime.ToUniversalTime(),
            EndTime = dto.EndTime.ToUniversalTime(),
            Capacity = dto.Capacity,
            ImageUrl = dto.ImageUrl,
            OrganizerId = GetUserId(),
            Status = EventStatus.Pending
        };

        var created = await _eventService.CreateEventAsync(ev);
        if (created == null) return StatusCode(500, "Failed to create event");

        return CreatedAtAction(nameof(GetEvent), new { id = created.Id }, MapToResponse(created));
    }

    // PUT /api/events/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] UpdateEventDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime must be after StartTime");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        // Only the organizer or an admin/manager may edit.
        if (ev.OrganizerId != GetUserId() && !IsAdminOrManager())
            return Forbid();

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Location = dto.Location;
        ev.VenueId = dto.VenueId;
        ev.StartTime = dto.StartTime.ToUniversalTime();
        ev.EndTime = dto.EndTime.ToUniversalTime();
        ev.Capacity = dto.Capacity;
        ev.ImageUrl = dto.ImageUrl;

        var success = await _eventService.UpdateEventAsync(ev);
        if (!success) return StatusCode(500, "Failed to update event");

        return Ok(MapToResponse(ev));
    }

    // DELETE /api/events/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (ev.OrganizerId != GetUserId() && !IsAdminOrManager())
            return Forbid();

        var success = await _eventService.DeleteEventAsync(id, tenantId);
        if (!success) return StatusCode(500, "Failed to delete event");

        return NoContent();
    }

    // POST /api/events/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveEvent(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");
        if (!IsAdminOrManager()) return Forbid();

        var success = await _eventService.ApproveEventAsync(id, tenantId, GetUserId());
        if (!success) return NotFound("Event not found");

        return Ok(new { message = "Event approved" });
    }

    // POST /api/events/{id}/reject
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectEvent(string id, [FromBody] RejectEventDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");
        if (!IsAdminOrManager()) return Forbid();

        var success = await _eventService.RejectEventAsync(id, tenantId, GetUserId(), dto.Reason);
        if (!success) return NotFound("Event not found");

        return Ok(new { message = "Event rejected" });
    }

    /// <summary>
    /// Xuất file lịch .ics cho sự kiện
    /// </summary>
    /// <param name="id">ID của sự kiện</param>
    /// <returns>File iCalendar (.ics)</returns>
    /// <response code="200">Trả về file .ics</response>
    /// <response code="400">Nếu ID sự kiện không hợp lệ</response>
    /// <response code="404">Nếu không tìm thấy sự kiện</response>
    [HttpGet("{id}/calendar.ics")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadIcs(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid event ID");
        }

        var tenantId = await ResolveTenantIdAsync();
        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null)
        {
            return NotFound("Event not found");
        }

        var icsBytes = _calendarService.GenerateEventIcs(ev);
        return File(icsBytes, "text/calendar", $"{ev.Id}.ics");
    }

    // ============ HELPERS ============

    private async Task<string> ResolveTenantIdAsync()
    {
        var claimTenantId = GetTenantId();
        if (!string.IsNullOrEmpty(claimTenantId)) return claimTenantId;

        var subdomain = HttpContext.Items["Subdomain"]?.ToString();
        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = await _tenantService.GetTenantBySubdomainAsync(subdomain);
            if (tenant != null) return tenant.Id;
        }

        if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdHeader))
        {
            return tenantIdHeader.ToString();
        }

        return "default-tenant";
    }

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() =>
        User.IsInRole("admin") || User.IsInRole("manager") ||
        User.IsInRole("Admin") || User.IsInRole("Manager");

    private static EventResponseDto MapToResponse(Event ev) => new()
    {
        Id = ev.Id,
        TenantId = ev.TenantId,
        Title = ev.Title,
        Description = ev.Description,
        Location = ev.Location,
        VenueId = ev.VenueId,
        StartTime = ev.StartTime,
        EndTime = ev.EndTime,
        Capacity = ev.Capacity,
        ImageUrl = ev.ImageUrl,
        OrganizerId = ev.OrganizerId,
        Status = (int)ev.Status,
        StatusName = ev.Status.ToString(),
        ApprovedById = ev.ApprovedById,
        ApprovedAt = ev.ApprovedAt,
        RejectionReason = ev.RejectionReason,
        CreatedAt = ev.CreatedAt,
        UpdatedAt = ev.UpdatedAt
    };
}
