using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AgendaController : ControllerBase
{
    private readonly IAgendaService _agendaService;
    private readonly IEventService _eventService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AgendaController> _logger;

    public AgendaController(
        IAgendaService agendaService, 
        IEventService eventService, 
        ITenantService tenantService,
        ILogger<AgendaController> logger)
    {
        _agendaService = agendaService;
        _eventService = eventService;
        _tenantService = tenantService;
        _logger = logger;
    }

    // GET /api/events/{eventId}/agenda (Public)
    [HttpGet("events/{eventId}/agenda")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAgenda(string eventId)
    {
        var tenantId = await ResolveTenantIdAsync();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        _logger.LogInformation($"Fetching agenda for event {eventId} and tenant {tenantId}");
        var agenda = await _agendaService.GetAgendaByEventAsync(eventId, tenantId);

        return Ok(agenda.Select(MapToDto));
    }

    // POST /api/events/{eventId}/agenda (Organizer/Admin)
    [HttpPost("events/{eventId}/agenda")]
    public async Task<IActionResult> CreateAgendaItem(string eventId, [FromBody] CreateAgendaDto dto)
    {
        var tenantId = GetTenantIdFromClaims();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        // Verify event ownership or existence
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return NotFound("Event not found");

        // Check if user is organizer or admin
        if (ev.OrganizerId != GetUserIdFromClaims() && !IsAdminOrManager())
            return Forbid();

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime must be after StartTime");

        var item = new AgendaItem
        {
            TenantId = tenantId,
            EventId = eventId,
            StartTime = dto.StartTime.ToUniversalTime(),
            EndTime = dto.EndTime.ToUniversalTime(),
            Title = dto.Title,
            Description = dto.Description,
            Speaker = dto.Speaker,
            MaterialUrl = dto.MaterialUrl,
            Order = dto.Order
        };

        var created = await _agendaService.CreateAgendaItemAsync(item);
        if (created == null) return StatusCode(500, "Failed to create agenda item");

        return Ok(MapToDto(created));
    }

    // PUT /api/agenda/{id} (Organizer/Admin)
    [HttpPut("agenda/{id}")]
    public async Task<IActionResult> UpdateAgendaItem(string id, [FromBody] UpdateAgendaDto dto)
    {
        var tenantId = GetTenantIdFromClaims();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var item = await _agendaService.GetAgendaItemByIdAsync(id, tenantId);
        if (item == null) return NotFound("Agenda item not found");

        var ev = await _eventService.GetEventByIdAsync(item.EventId, tenantId);
        if (ev == null) return NotFound("Associated event not found");

        if (ev.OrganizerId != GetUserIdFromClaims() && !IsAdminOrManager())
            return Forbid();

        if (dto.EndTime <= dto.StartTime)
            return BadRequest("EndTime must be after StartTime");

        item.Title = dto.Title;
        item.Description = dto.Description;
        item.Speaker = dto.Speaker;
        item.StartTime = dto.StartTime.ToUniversalTime();
        item.EndTime = dto.EndTime.ToUniversalTime();
        item.MaterialUrl = dto.MaterialUrl;
        item.Order = dto.Order;

        var success = await _agendaService.UpdateAgendaItemAsync(item);
        if (!success) return StatusCode(500, "Failed to update agenda item");

        return Ok(MapToDto(item));
    }

    // DELETE /api/agenda/{id} (Organizer/Admin)
    [HttpDelete("agenda/{id}")]
    public async Task<IActionResult> DeleteAgendaItem(string id)
    {
        var tenantId = GetTenantIdFromClaims();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var item = await _agendaService.GetAgendaItemByIdAsync(id, tenantId);
        if (item == null) return NotFound("Agenda item not found");

        var ev = await _eventService.GetEventByIdAsync(item.EventId, tenantId);
        if (ev == null) return NotFound("Associated event not found");

        if (ev.OrganizerId != GetUserIdFromClaims() && !IsAdminOrManager())
            return Forbid();

        var success = await _agendaService.DeleteAgendaItemAsync(id, tenantId);
        if (!success) return StatusCode(500, "Failed to delete agenda item");

        return NoContent();
    }

    // ============ HELPERS ============

    private async Task<string> ResolveTenantIdAsync()
    {
        var claimTenantId = GetTenantIdFromClaims();
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

    private string GetTenantIdFromClaims() => User.FindFirst("tenantId")?.Value ?? string.Empty;

    private string GetUserIdFromClaims() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private bool IsAdminOrManager() =>
        User.IsInRole("admin") || User.IsInRole("manager") ||
        User.IsInRole("Admin") || User.IsInRole("Manager");

    private static AgendaItemDto MapToDto(AgendaItem item) => new()
    {
        Id = item.Id,
        TenantId = item.TenantId,
        EventId = item.EventId,
        StartTime = item.StartTime,
        EndTime = item.EndTime,
        Title = item.Title,
        Description = item.Description,
        Speaker = item.Speaker,
        MaterialUrl = item.MaterialUrl,
        Order = item.Order
    };
}
