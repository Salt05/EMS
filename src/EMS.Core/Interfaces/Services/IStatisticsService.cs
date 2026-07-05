using EMS.Shared.DTOs;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Aggregates dashboard statistics for the three admin scopes:
/// SuperAdmin (cross-tenant), TenantAdmin (single tenant) and Organizer (own events).
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// System-wide statistics across every tenant. SuperAdmin only.
    /// </summary>
    Task<SuperAdminDashboardStatsDto> GetSuperAdminStatsAsync();

    /// <summary>
    /// Statistics scoped to a single tenant. TenantAdmin.
    /// </summary>
    Task<TenantAdminDashboardStatsDto> GetTenantAdminStatsAsync(string tenantId);

    /// <summary>
    /// Statistics scoped to the events organised by a single user. Organizer/Manager.
    /// </summary>
    Task<OrganizerDashboardStatsDto> GetOrganizerStatsAsync(string tenantId, string organizerId);
}
