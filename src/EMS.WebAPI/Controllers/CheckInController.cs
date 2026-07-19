using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.CheckIns;
using EMS.Shared.DTOs.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckInController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly ILogger<CheckInController> _logger;

    public CheckInController(
        IRegistrationService registrationService,
        IEventService eventService,
        IUserService userService,
        ILogger<CheckInController> logger)
    {
        _registrationService = registrationService;
        _eventService = eventService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Current user generates a check-in code for their confirmed registration.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(CheckInResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Generate([FromBody] GenerateCheckInDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var (reg, error) = await _registrationService.GenerateCheckInCodeAsync(dto.EventId, tenantId, GetUserId());
        if (reg == null) return BadRequest(error ?? "Failed to generate check-in code");

        return Ok(MapToResponse(reg));
    }

    /// <summary>
    /// Organizer/admin validates a check-in code and marks attendance.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(CheckInResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Validate([FromBody] ValidateCheckInDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var (reg, error) = await _registrationService.ValidateCheckInAsync(
            dto.Code, tenantId, GetUserId(), IsAdminOrManager());

        if (error == IRegistrationService.ForbiddenError) return Forbid();
        if (reg == null) return BadRequest(error ?? "Invalid check-in code");

        _logger.LogInformation($"User {GetUserId()} checked in registration {reg.Id}");
        return Ok(MapToResponse(reg));
    }

    /// <summary>
    /// Gets all checked-in registrations for an event.
    /// </summary>
    [HttpGet("event/{eventId}/attendees")]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttendees(string eventId)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return NotFound("Event not found");

        if (!CanManageEvent(ev)) return Forbid();

        var regs = await _registrationService.GetRegistrationsByEventAsync(eventId, tenantId);
        var checkedInRegs = regs.Where(r => r.CheckedIn).ToList();

        var users = await _userService.GetUsersByTenantAsync(tenantId);
        var userMap = users.ToDictionary(u => u.Id);

        var dtos = checkedInRegs.Select(r =>
        {
            var dto = MapToRegistrationResponse(r);
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

    // ============ HELPERS ============

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserId() => User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() => User.Claims.Any(c => 
        (c.Type == "role" || c.Type == ClaimTypes.Role) && 
        (c.Value.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
         c.Value.Equals("manager", StringComparison.OrdinalIgnoreCase) ||
         c.Value.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
    );

    private bool CanManageEvent(Event ev) => ev.OrganizerId == GetUserId() || IsAdminOrManager();

    private static CheckInResponseDto MapToResponse(Registration reg) => new()
    {
        RegistrationId = reg.Id,
        EventId = reg.EventId,
        UserId = reg.UserId,
        CheckInCode = reg.CheckInCode,
        CheckInCodeExpiresAt = reg.CheckInCodeExpiresAt,
        CheckedIn = reg.CheckedIn,
        CheckedInAt = reg.CheckedInAt
    };

    private static RegistrationResponseDto MapToRegistrationResponse(Registration reg) => new()
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
