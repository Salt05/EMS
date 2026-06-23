namespace EMS.Shared.DTOs.Admin;

/// <summary>
/// Phản hồi danh sách user có phân trang.
/// </summary>
public class AdminUserListResponseDto
{
    public List<AdminUserItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Thông tin user trong danh sách admin.
/// </summary>
public class AdminUserItemDto
{
    public string Id { get; set; } = string.Empty;
    public string? MSSV { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public List<string> RoleIds { get; set; } = new();
    public string TenantId { get; set; } = string.Empty;
    public string? TenantName { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request tạo user mới từ admin dashboard.
/// </summary>
public class AdminCreateUserRequestDto
{
    public string? MSSV { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string Password { get; set; } = string.Empty;
    public string RoleId { get; set; } = "employee";
    public string? TenantId { get; set; }
}

/// <summary>
/// Request cập nhật thông tin user.
/// </summary>
public class AdminUpdateUserRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
}

/// <summary>
/// Request thay đổi role (chỉ SuperAdmin).
/// </summary>
public class AdminUpdateRoleRequestDto
{
    public string RoleId { get; set; } = string.Empty;
}

/// <summary>
/// Request chuyển tenant (chỉ SuperAdmin).
/// </summary>
public class AdminChangeTenantRequestDto
{
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Filter/query params cho danh sách user.
/// </summary>
public class AdminUserFilterDto
{
    public string? Search { get; set; }
    public string? RoleId { get; set; }
    public string? TenantId { get; set; }
    public int? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
