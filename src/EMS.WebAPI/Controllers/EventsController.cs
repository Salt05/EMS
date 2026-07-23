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
    private readonly IUserService _userService;
    private readonly IAgendaService _agendaService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, IUserService userService, IAgendaService agendaService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _userService = userService;
        _agendaService = agendaService;
        _logger = logger;
    }

    // GET /api/events?status=1
    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] EventStatus? status = null)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var events = await _eventService.GetEventsByTenantAsync(tenantId, status);
        
        // Batch fetch users to populate organizer email
        var userDict = new Dictionary<string, string>();
        try
        {
            var tenantUsers = await _userService.GetUsersByTenantAsync(tenantId);
            userDict = tenantUsers.ToDictionary(u => u.Id, u => u.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to batch load user emails for events");
        }

        var responseList = events.Select(ev =>
        {
            var email = userDict.TryGetValue(ev.OrganizerId, out var e) ? e : string.Empty;
            return MapToResponse(ev, email);
        });

        return Ok(responseList);
    }

    // GET /api/events/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        var email = string.Empty;
        if (!string.IsNullOrEmpty(ev.OrganizerId))
        {
            var user = await _userService.GetUserByIdAsync(ev.OrganizerId, tenantId);
            if (user != null) email = user.Email;
        }

        return Ok(MapToResponse(ev, email));
    }

    // POST /api/events
    [HttpPost]
    [Authorize(Roles = "manager,admin,superadmin")]
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
            Status = EventStatus.Pending,
            Price = dto.Price,
            Scope = (EventScope)dto.Scope,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _eventService.CreateEventAsync(ev);

        if (dto.AgendaItems != null && dto.AgendaItems.Any())
        {
            foreach (var itemDto in dto.AgendaItems)
            {
                var item = new AgendaItem
                {
                    TenantId = tenantId,
                    EventId = created.Id,
                    StartTime = itemDto.StartTime.ToUniversalTime(),
                    EndTime = itemDto.EndTime.ToUniversalTime(),
                    Title = itemDto.Title
                };
                await _agendaService.CreateAgendaItemAsync(item);
            }
        }

        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return CreatedAtAction(nameof(GetEvent), new { id = created.Id }, MapToResponse(created, email));
    }

    // PUT /api/events/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(string id, [FromBody] UpdateEventDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (ev.OrganizerId != GetUserId() && !IsAdminOrManager())
            return Forbid();

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime must be after StartTime");

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Location = dto.Location;
        ev.VenueId = dto.VenueId;
        ev.StartTime = dto.StartTime.ToUniversalTime();
        ev.EndTime = dto.EndTime.ToUniversalTime();
        ev.Capacity = dto.Capacity;
        ev.ImageUrl = dto.ImageUrl;
        if (ev.Status != EventStatus.Pending && dto.Price != ev.Price)
            return BadRequest("Event fee cannot be changed after approval");

        ev.Price = dto.Price;
        ev.Scope = (EventScope)dto.Scope;
        ev.UpdatedAt = DateTime.UtcNow;

        var success = await _eventService.UpdateEventAsync(ev);
        if (!success) return StatusCode(500, "Failed to update event");

        return NoContent();
    }

    // POST /api/events/{id}/generate-code
    [HttpPost("{id}/generate-code")]
    public async Task<IActionResult> GenerateCheckInCode(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (ev.OrganizerId != GetUserId() && !IsAdminOrManager())
            return Forbid();

        // Generate 6-digit code valid for 15 mins
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        ev.CheckInCode = code;
        ev.CheckInCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);

        var success = await _eventService.UpdateEventAsync(ev);
        if (!success) return StatusCode(500, "Failed to generate event check-in code");

        return Ok(new { CheckInCode = code, ExpiresAt = ev.CheckInCodeExpiresAt });
    }

    // POST /api/events/{id}/expire-code
    [HttpPost("{id}/expire-code")]
    public async Task<IActionResult> ExpireCheckInCode(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(id, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (ev.OrganizerId != GetUserId() && !IsAdminOrManager())
            return Forbid();

        // Clear the check-in code
        ev.CheckInCode = null;
        ev.CheckInCodeExpiresAt = null;

        var success = await _eventService.UpdateEventAsync(ev);
        if (!success) return StatusCode(500, "Failed to expire event check-in code");

        return Ok(new { message = "Code expired successfully" });
    }

    // POST /api/events/{id}/approve
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveEvent(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");
        if (!IsAdminOrSuperAdmin()) return Forbid();

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
        if (!IsAdminOrSuperAdmin()) return Forbid();

        var success = await _eventService.RejectEventAsync(id, tenantId, GetUserId(), dto.Reason);
        if (!success) return NotFound("Event not found");

        return Ok(new { message = "Event rejected" });
    }

    // ============ HELPERS ============

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserId() => User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() => User.Claims.Any(c => 
        (c.Type == "role" || c.Type == ClaimTypes.Role) && 
        (c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
         c.Value.Equals("manager", StringComparison.OrdinalIgnoreCase) ||
         c.Value.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
    );

    private bool IsAdminOrSuperAdmin() => User.Claims.Any(c =>
        (c.Type == "role" || c.Type == ClaimTypes.Role) &&
        (c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
         c.Value.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
    );

    private static EventResponseDto MapToResponse(Event ev, string organizerEmail = "") => new()
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
        OrganizerEmail = organizerEmail,
        Status = (int)ev.Status,
        StatusName = ev.Status.ToString(),
        ApprovedById = ev.ApprovedById,
        ApprovedAt = ev.ApprovedAt,
        RejectionReason = ev.RejectionReason,
        CheckInCode = ev.CheckInCode,
        CheckInCodeExpiresAt = ev.CheckInCodeExpiresAt,
        CreatedAt = ev.CreatedAt,
        UpdatedAt = ev.UpdatedAt,
        Price = ev.Price,
        Scope = (int)ev.Scope,
        ScopeName = ev.Scope.ToString()
    };
}
