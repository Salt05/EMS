using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants()
    {
        try
        {
            var tenants = await _tenantService.GetTenantsAsync();
            var tenantDTOs = tenants.Select(t => new TenantDTO
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                Address = t.Address,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            }).ToList();

            return Ok(tenantDTOs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants list in API");
            return StatusCode(500, "Internal server error");
        }
    }
}
