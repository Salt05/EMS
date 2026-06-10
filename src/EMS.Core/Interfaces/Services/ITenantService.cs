using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

public interface ITenantService
{
    Task<Tenant?> GetTenantByIdAsync(string tenantId);
    Task<Tenant?> GetTenantBySubdomainAsync(string subdomain);
    Task<Tenant?> CreateTenantAsync(Tenant tenant);
    Task<bool> UpdateTenantAsync(Tenant tenant);
    Task<bool> DeleteTenantAsync(string tenantId);
}
