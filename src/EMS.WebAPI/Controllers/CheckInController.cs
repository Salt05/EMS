using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.CheckIns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckInController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<CheckInController> _logger;

    public CheckInController(
        IRegistrationService registrationService,
        ILogger<CheckInController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    // POST /api/checkin/generate — current user generates a check-in code for their confirmed registration
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateCheckInDto dto)
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var (reg, error) = await _registrationService.GenerateCheckInCodeAsync(dto.EventId, tenantId, GetUserId());
        if (reg == null) return BadRequest(error ?? "Failed to generate check-in code");

        return Ok(MapToResponse(reg));
    }

    // POST /api/checkin/validate — organizer/admin validates a code and marks attendance
    [HttpPost("validate")]
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

    // ============ HELPERS ============

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() =>
        User.IsInRole("admin") || User.IsInRole("manager") ||
        User.IsInRole("Admin") || User.IsInRole("Manager");

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
}
