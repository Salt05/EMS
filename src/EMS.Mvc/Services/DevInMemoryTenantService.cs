using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// Tenant service dùng dữ liệu mẫu cố định khi chạy Development không cần Firebase.
/// </summary>
public class DevInMemoryTenantService : ITenantService
{
    public const string DefaultTenantId = "default-tenant";
    public const string PublicTenantId = "tenant-public";

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

    private static readonly Tenant PublicTenant = new()
    {
        Id = PublicTenantId,
        Name = "Thành viên tự do / Khách ngoài",
        Subdomain = "community",
        Email = "guest@ems.local",
        Address = "Tài khoản công khai / Khách ngoài",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public Task<Tenant?> GetTenantByIdAsync(string tenantId)
    {
        if (tenantId == DefaultTenantId) return Task.FromResult<Tenant?>(Clone(DefaultTenant));
        if (tenantId == PublicTenantId) return Task.FromResult<Tenant?>(Clone(PublicTenant));
        return Task.FromResult<Tenant?>(null);
    }

    public Task<Tenant?> GetTenantBySubdomainAsync(string subdomain)
    {
        if (subdomain.Equals("default", StringComparison.OrdinalIgnoreCase)) return Task.FromResult<Tenant?>(Clone(DefaultTenant));
        if (subdomain.Equals("community", StringComparison.OrdinalIgnoreCase) || subdomain.Equals("public", StringComparison.OrdinalIgnoreCase)) return Task.FromResult<Tenant?>(Clone(PublicTenant));
        return Task.FromResult<Tenant?>(null);
    }

    public Task<Tenant?> CreateTenantAsync(Tenant tenant) => Task.FromResult<Tenant?>(tenant);

    public Task<bool> UpdateTenantAsync(Tenant tenant) => Task.FromResult(true);

    public Task<bool> DeleteTenantAsync(string tenantId) => Task.FromResult(false);

    public Task<List<Tenant>> GetTenantsAsync() => Task.FromResult(new List<Tenant> { Clone(DefaultTenant), Clone(PublicTenant) });

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
