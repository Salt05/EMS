using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// Tenant service dùng dữ liệu mẫu cố định khi chạy Development không cần Firebase.
/// Bao gồm 3 trường: HUFLIT, HCMUTE, HCMUT.
/// </summary>
public class DevInMemoryTenantService : ITenantService
{
    public const string DefaultTenantId = "default-tenant";

    // ─── Tenant IDs cho 3 trường ───────────────────────────────────────────
    public const string HuflitTenantId = "tenant-huflit";
    public const string HcmuteTenantId = "tenant-hcmute";
    public const string HcmutTenantId  = "tenant-hcmut";

    // ─── Mapping domain email → tenantId ───────────────────────────────────
    private static readonly Dictionary<string, string> DomainToTenantId = new(StringComparer.OrdinalIgnoreCase)
    {
        { "huflit.edu.vn",  HuflitTenantId  },
        { "hcmute.edu.vn",  HcmuteTenantId  },
        { "hcmut.edu.vn",   HcmutTenantId   },
    };

    private static readonly HashSet<string> PublicDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "live.com", "icloud.com", "mail.com"
    };

    // ─── Danh sách Tenant mẫu ──────────────────────────────────────────────
    private static readonly List<Tenant> _tenants = new()
    {
        new()
        {
            Id        = HuflitTenantId,
            Name      = "ĐH Ngoại ngữ - Tin học HUFLIT",
            Subdomain = "huflit",
            Email     = "contact@huflit.edu.vn",
            Address   = "828 Sư Vạn Hạnh, Phường 13, Quận 10, TP.HCM",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new()
        {
            Id        = HcmuteTenantId,
            Name      = "ĐH Sư phạm Kỹ thuật TP.HCM",
            Subdomain = "hcmute",
            Email     = "contact@hcmute.edu.vn",
            Address   = "01 Võ Văn Ngân, Thủ Đức, TP.HCM",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new()
        {
            Id        = HcmutTenantId,
            Name      = "ĐH Bách Khoa TP.HCM",
            Subdomain = "hcmut",
            Email     = "contact@hcmut.edu.vn",
            Address   = "268 Lý Thường Kiệt, Phường 14, Quận 10, TP.HCM",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        // Default fallback (chỉ dùng nội bộ nếu không resolve được)
        new()
        {
            Id        = DefaultTenantId,
            Name      = "EMS Portal",
            Subdomain = "default",
            Email     = "contact@ems.com",
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    };

    // ─── Resolve domain email → tenantId ──────────────────────────────────
    public static string? ResolveTenantIdFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var atIdx = email.IndexOf('@');
        if (atIdx < 0) return null;
        var domain = email.Substring(atIdx + 1).Trim().ToLowerInvariant();

        if (DomainToTenantId.TryGetValue(domain, out var tid))
        {
            return tid;
        }

        string resolvedName;
        if (PublicDomains.Contains(domain))
        {
            var dotIdx = domain.IndexOf('.');
            var domainPrefix = dotIdx > 0 ? domain.Substring(0, dotIdx) : domain;
            resolvedName = $"Cộng đồng {char.ToUpper(domainPrefix[0])}{domainPrefix.Substring(1)}";
        }
        else
        {
            // Tự động trích xuất tên trường từ email domain không phải public
            var domainParts = domain.Split('.');
            string name = domainParts[0].ToUpperInvariant();
            if (name == "STUDENT" || name == "SV" || name == "MAIL" || name == "STUDENTS")
            {
                if (domainParts.Length > 1) name = domainParts[1].ToUpperInvariant();
            }
            resolvedName = $"Trường Đại học {name}";
        }

        return GetOrCreateTenant(resolvedName);
    }

    public static string GetOrCreateTenant(string? nameOrId)
    {
        if (string.IsNullOrWhiteSpace(nameOrId))
        {
            return HuflitTenantId;
        }

        nameOrId = nameOrId.Trim();

        // 1. Khớp Id
        var existingById = _tenants.FirstOrDefault(t => t.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
        if (existingById != null)
        {
            return existingById.Id;
        }

        // 2. Khớp Name
        var existingByName = _tenants.FirstOrDefault(t => t.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
        if (existingByName != null)
        {
            return existingByName.Id;
        }

        // 3. Tạo mới động
        string slug = GenerateSlug(nameOrId);
        string newId = $"tenant-{slug}";

        var existingBySlug = _tenants.FirstOrDefault(t => t.Id.Equals(newId, StringComparison.OrdinalIgnoreCase));
        if (existingBySlug != null)
        {
            return existingBySlug.Id;
        }

        var newTenant = new Tenant
        {
            Id = newId,
            Name = nameOrId,
            Subdomain = slug,
            Email = $"contact@{slug}.edu.vn",
            Address = "Việt Nam",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _tenants.Add(newTenant);

        // Đảm bảo có sự kiện mẫu cho trường mới tạo
        DevInMemoryEventService.EnsureEventsForTenant(newId, nameOrId);

        return newId;
    }

    private static string GenerateSlug(string phrase)
    {
        string str = phrase.ToLowerInvariant();
        System.Text.StringBuilder sb = new();
        foreach (char c in str)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '-')
            {
                sb.Append('-');
            }
        }
        string result = sb.ToString();
        while (result.Contains("--"))
        {
            result = result.Replace("--", "-");
        }
        return result.Trim('-');
    }

    public Task<Tenant?> GetTenantByIdAsync(string tenantId) =>
        Task.FromResult(_tenants.FirstOrDefault(t => t.Id == tenantId) is { } t ? Clone(t) : null);

    public Task<Tenant?> GetTenantBySubdomainAsync(string subdomain) =>
        Task.FromResult(_tenants.FirstOrDefault(t => t.Subdomain.Equals(subdomain, StringComparison.OrdinalIgnoreCase)) is { } t ? Clone(t) : null);

    public Task<Tenant?> CreateTenantAsync(Tenant tenant) => Task.FromResult<Tenant?>(tenant);

    public Task<bool> UpdateTenantAsync(Tenant tenant) => Task.FromResult(true);

    public Task<bool> DeleteTenantAsync(string tenantId) => Task.FromResult(false);

    public Task<List<Tenant>> GetTenantsAsync() =>
        Task.FromResult(_tenants.Select(Clone).ToList());

    private static Tenant Clone(Tenant source) => new()
    {
        Id          = source.Id,
        Name        = source.Name,
        Subdomain   = source.Subdomain,
        Email       = source.Email,
        PhoneNumber = source.PhoneNumber,
        Address     = source.Address,
        CreatedAt   = source.CreatedAt,
        UpdatedAt   = source.UpdatedAt,
        IsActive    = source.IsActive
    };
}
