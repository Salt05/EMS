using System.Security.Claims;
using EMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

/// <summary>
/// Dashboard statistics endpoints for the SuperAdmin, TenantAdmin and Organizer portals.
/// Routes are declared at method level to match the paths the Blazor clients call.
/// </summary>
[ApiController]
[Authorize]
public class DashboardStatsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<DashboardStatsController> _logger;

    public DashboardStatsController(IStatisticsService statisticsService, ILogger<DashboardStatsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    // GET /api/superadmin/dashboard/stats
    [HttpGet("api/superadmin/dashboard/stats")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> GetSuperAdminStats()
    {
        try
        {
            return Ok(await _statisticsService.GetSuperAdminStatsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building superadmin dashboard stats");
            return StatusCode(500, "Failed to build statistics");
        }
    }

    // GET /api/admin/dashboard/stats
    [HttpGet("api/admin/dashboard/stats")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> GetTenantAdminStats()
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        try
        {
            return Ok(await _statisticsService.GetTenantAdminStatsAsync(tenantId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building tenant admin dashboard stats for {TenantId}", tenantId);
            return StatusCode(500, "Failed to build statistics");
        }
    }

    // GET /api/organizer/dashboard/stats
    [HttpGet("api/organizer/dashboard/stats")]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> GetOrganizerStats()
    {
        var tenantId = GetTenantId();
        if (string.IsNullOrEmpty(tenantId)) return BadRequest("Invalid tenant");

        try
        {
            return Ok(await _statisticsService.GetOrganizerStatsAsync(tenantId, GetUserId()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building organizer dashboard stats");
            return StatusCode(500, "Failed to build statistics");
        }
    }

    private string GetTenantId() => User.FindFirst("tenantId")?.Value ?? string.Empty;
    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
}
