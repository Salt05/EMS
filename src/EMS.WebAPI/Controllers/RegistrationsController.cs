using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        IRegistrationService registrationService,
        IEventService eventService,
        IUserService userService,
        ILogger<RegistrationsController> logger)
    {
        _registrationService = registrationService;
        _eventService = eventService;
        _userService = userService;
        _logger = logger;
    }

    // GET /api/registrations/me — current user's registrations
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRegistrations()
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var regs = await _registrationService.GetRegistrationsByUserAsync(GetUserId(), tenantId);
        return Ok(regs.Select(MapToResponse));
    }

    // GET /api/registrations/event/{eventId}?status=2 — registrations of an event (organizer/admin only)
    [HttpGet("event/{eventId}")]
    public async Task<IActionResult> GetEventRegistrations(string eventId, [FromQuery] RegistrationStatus? status = null)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (!CanManageEvent(ev)) return Forbid();

        var regs = await _registrationService.GetRegistrationsByEventAsync(eventId, tenantId, status);
        
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        var userMap = users.ToDictionary(u => u.Id);

        var dtos = regs.Select(r =>
        {
            var dto = MapToResponse(r);
            if (userMap.TryGetValue(r.UserId, out var u))
            {
                dto.UserFullName = u.FullName;
                dto.UserEmail = u.Email;
                dto.UserMSSV = u.MSSV;
            }
            return dto;
        }).ToList();

        return Ok(dtos);
    }

    // GET /api/registrations/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRegistration(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var reg = await _registrationService.GetRegistrationByIdAsync(id, tenantId);
        if (reg == null) return NotFound("Registration not found");

        // The owner, or an organizer/admin of the event, may view it.
        if (reg.UserId != GetUserId())
        {
            var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
            if (ev == null || !CanManageEvent(ev)) return Forbid();
        }

        var dto = MapToResponse(reg);
        var user = await _userService.GetUserByIdAsync(reg.UserId, tenantId);
        if (user != null)
        {
            dto.UserFullName = user.FullName;
            dto.UserEmail = user.Email;
            dto.UserMSSV = user.MSSV;
        }

        return Ok(dto);
    }

    // POST /api/registrations — register the current user for an event
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] CreateRegistrationDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var (reg, error) = await _registrationService.RegisterAsync(dto.EventId, tenantId, GetUserId(), dto.Note);
        if (reg == null) return BadRequest(error ?? "Failed to register");

        return CreatedAtAction(nameof(GetRegistration), new { id = reg.Id }, MapToResponse(reg));
    }

    // POST /api/registrations/{id}/cancel — owner (or organizer/admin) cancels a registration
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var reg = await _registrationService.GetRegistrationByIdAsync(id, tenantId);
        if (reg == null) return NotFound("Registration not found");

        if (reg.UserId != GetUserId())
        {
            var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
            if (ev == null || !CanManageEvent(ev)) return Forbid();
        }

        var success = await _registrationService.CancelAsync(id, tenantId);
        if (!success) return BadRequest("Registration cannot be cancelled");

        return Ok(new { message = "Registration cancelled" });
    }

    // POST /api/registrations/{id}/approve — organizer/admin confirms a registration
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(string id)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var reg = await _registrationService.GetRegistrationByIdAsync(id, tenantId);
        if (reg == null) return NotFound("Registration not found");

        var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
        if (ev == null) return NotFound("Event not found");
        if (!CanManageEvent(ev)) return Forbid();

        var success = await _registrationService.ApproveAsync(id, tenantId, GetUserId());
        if (!success) return StatusCode(500, "Failed to approve registration");

        return Ok(new { message = "Registration approved" });
    }

    // POST /api/registrations/{id}/reject — organizer/admin rejects a registration
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(string id, [FromBody] RejectRegistrationDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var reg = await _registrationService.GetRegistrationByIdAsync(id, tenantId);
        if (reg == null) return NotFound("Registration not found");

        var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
        if (ev == null) return NotFound("Event not found");
        if (!CanManageEvent(ev)) return Forbid();

        var success = await _registrationService.RejectAsync(id, tenantId, GetUserId(), dto.Reason);
        if (!success) return StatusCode(500, "Failed to reject registration");

        return Ok(new { message = "Registration rejected" });
    }

    // ============ HELPERS ============

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() =>
        User.IsInRole("admin") || User.IsInRole("manager");

    // The event's organizer, or an admin/manager, may manage its registrations.
    private bool CanManageEvent(Event ev) => ev.OrganizerId == GetUserId() || IsAdminOrManager();

    private static RegistrationResponseDto MapToResponse(Registration reg) => new()
    {
        Id = reg.Id,
        TenantId = reg.TenantId,
        EventId = reg.EventId,
        UserId = reg.UserId,
        Note = reg.Note,
        Status = (int)reg.Status,
        StatusName = reg.Status.ToString(),
        RegisteredAt = reg.RegisteredAt,
        ProcessedById = reg.ProcessedById,
        ProcessedAt = reg.ProcessedAt,
        RejectionReason = reg.RejectionReason,
        CancelledAt = reg.CancelledAt,
        CheckedIn = reg.CheckedIn,
        CheckedInAt = reg.CheckedInAt,
        CheckInCode = reg.CheckInCode,
        CheckInCodeExpiresAt = reg.CheckInCodeExpiresAt,
        CreatedAt = reg.CreatedAt,
        UpdatedAt = reg.UpdatedAt
    };
}
