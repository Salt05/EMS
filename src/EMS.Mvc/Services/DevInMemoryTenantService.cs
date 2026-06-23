using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// Tenant service dùng dữ liệu mẫu cố định khi chạy Development không cần Firebase.
/// </summary>
public class DevInMemoryTenantService : ITenantService
{
    public const string DefaultTenantId = "default-tenant";

    private static readonly Tenant DefaultTenant = new()
    {
        Id = DefaultTenantId,
        Name = "EMS Portal",
        Subdomain = "default",
        Email = "contact@ems.com",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public Task<Tenant?> GetTenantByIdAsync(string tenantId) =>
        Task.FromResult(tenantId == DefaultTenantId ? Clone(DefaultTenant) : null);

    public Task<Tenant?> GetTenantBySubdomainAsync(string subdomain) =>
        Task.FromResult(subdomain.Equals("default", StringComparison.OrdinalIgnoreCase) ? Clone(DefaultTenant) : null);

    public Task<Tenant?> CreateTenantAsync(Tenant tenant) => Task.FromResult<Tenant?>(tenant);

    public Task<bool> UpdateTenantAsync(Tenant tenant) => Task.FromResult(true);

    public Task<bool> DeleteTenantAsync(string tenantId) => Task.FromResult(false);

    public Task<List<Tenant>> GetTenantsAsync() => Task.FromResult(new List<Tenant> { Clone(DefaultTenant) });

    private static Tenant Clone(Tenant source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Subdomain = source.Subdomain,
        Email = source.Email,
        PhoneNumber = source.PhoneNumber,
        Address = source.Address,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        IsActive = source.IsActive
    };
}
