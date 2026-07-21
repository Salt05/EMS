using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

public class SuperAdminManagementService : ISuperAdminManagementService
{
    private readonly ITenantService _tenantService;
    private readonly IAdminUserService _adminUserService;
    private readonly IEventService _eventService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SuperAdminManagementService> _logger;

    public SuperAdminManagementService(
        ITenantService tenantService,
        IAdminUserService adminUserService,
        IEventService eventService,
        IMemoryCache cache,
        ILogger<SuperAdminManagementService> logger)
    {
        _tenantService = tenantService;
        _adminUserService = adminUserService;
        _eventService = eventService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<TenantDTO>> GetTenantsWithStatsAsync()
    {
        var tenants = await _tenantService.GetTenantsAsync();
        var dtos = new List<TenantDTO>();

        foreach (var t in tenants)
        {
            var (users, totalUsers) = await _adminUserService.GetUsersAsync(t.Id, null, null, null, 1, 1);
            var events = await _eventService.GetEventsByTenantAsync(t.Id);

            dtos.Add(new TenantDTO
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                Address = t.Address,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UserCount = totalUsers,
                EventCount = events.Count
            });
        }

        return dtos.OrderBy(d => d.Name).ToList();
    }
}
