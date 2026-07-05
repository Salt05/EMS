using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

/// <summary>
/// Report export endpoints. Produces downloadable Excel/PDF files for event
/// registration lists and tenant-wide event summaries.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportExportService _exportService;
    private readonly IEventService _eventService;
    private readonly IRegistrationService _registrationService;
    private readonly IUserService _userService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportExportService exportService,
        IEventService eventService,
        IRegistrationService registrationService,
        IUserService userService,
        ITenantService tenantService,
        ILogger<ReportsController> logger)
    {
        _exportService = exportService;
        _eventService = eventService;
        _registrationService = registrationService;
        _userService = userService;
        _tenantService = tenantService;
        _logger = logger;
    }

    // GET /api/reports/events/{eventId}/registrations?format=excel|pdf
    // Exports the registration/attendee list for a single event.
    [HttpGet("events/{eventId}/registrations")]
    [Authorize(Roles = "manager,admin,superadmin")]
    public async Task<IActionResult> ExportEventRegistrations(string eventId, [FromQuery] string format = "excel")
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return NotFound("Event not found");
        if (!CanManageEvent(ev)) return Forbid();

        var regs = await _registrationService.GetRegistrationsByEventAsync(eventId, tenantId);
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        var userMap = users.ToDictionary(u => u.Id);

        var report = new RegistrationReport
        {
            EventTitle = ev.Title,
            Location = ev.Location,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            Capacity = ev.Capacity,
            Rows = regs
                .OrderBy(r => r.RegisteredAt)
                .Select((r, i) =>
                {
                    userMap.TryGetValue(r.UserId, out var u);
                    return new RegistrationReportRow
                    {
                        Index = i + 1,
                        FullName = u?.FullName ?? r.StudentName,
                        MSSV = u?.MSSV ?? string.Empty,
                        Email = u?.Email ?? r.StudentEmail,
                        StatusName = r.Status.ToString(),
                        CheckedIn = r.CheckedIn,
                        RegisteredAt = r.RegisteredAt,
                        CheckedInAt = r.CheckedInAt
                    };
                })
                .ToList()
        };

        var result = _exportService.ExportRegistrations(report, ParseFormat(format));
        return File(result.Content, result.ContentType, result.FileName);
    }

    // GET /api/reports/events/summary?format=excel|pdf
    // Exports a tenant-wide summary of every event with registration & check-in counts.
    [HttpGet("events/summary")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> ExportEventSummary([FromQuery] string format = "excel")
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        var events = await _eventService.GetEventsByTenantAsync(tenantId);
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        var userMap = users.ToDictionary(u => u.Id, u => u.FullName);
        var tenant = await _tenantService.GetTenantByIdAsync(tenantId);

        var rows = new List<EventSummaryRow>();
        int index = 1;
        foreach (var ev in events.OrderByDescending(e => e.StartTime))
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var active = regs.Where(r =>
                r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected).ToList();

            rows.Add(new EventSummaryRow
            {
                Index = index++,
                EventTitle = ev.Title,
                OrganizerName = userMap.TryGetValue(ev.OrganizerId, out var name) && !string.IsNullOrEmpty(name)
                    ? name
                    : ev.OrganizerId,
                StatusName = ev.Status.ToString(),
                StartTime = ev.StartTime,
                RegistrationCount = active.Count,
                CheckInCount = active.Count(r => r.CheckedIn)
            });
        }

        var report = new EventSummaryReport
        {
            TenantName = tenant?.Name ?? tenantId,
            GeneratedAt = DateTime.UtcNow,
            Rows = rows
        };

        var result = _exportService.ExportEventSummary(report, ParseFormat(format));
        return File(result.Content, result.ContentType, result.FileName);
    }

    // ============ HELPERS ============

    private static ExportFormat ParseFormat(string format) =>
        format?.Trim().ToLowerInvariant() is "pdf" ? ExportFormat.Pdf : ExportFormat.Excel;

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private bool IsAdminOrManager() => User.IsInRole("admin") || User.IsInRole("superadmin");
    private bool CanManageEvent(Event ev) => ev.OrganizerId == GetUserId() || IsAdminOrManager();
}
