using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Service quản lý user dành cho Admin Dashboard.
/// Hỗ trợ truy vấn cross-tenant (SuperAdmin) và single-tenant (TenantAdmin).
/// </summary>
public interface IAdminUserService
{
    /// <summary>
    /// Lấy danh sách user có phân trang và filter.
    /// Nếu tenantId != null thì chỉ lấy user trong tenant đó (TenantAdmin).
    /// Nếu tenantId == null thì lấy tất cả (SuperAdmin).
    /// </summary>
    Task<(List<User> Users, int TotalCount)> GetUsersAsync(
        string? tenantId, string? search, string? roleId, int? status, int page, int pageSize);

    /// <summary>
    /// Lấy user theo ID không giới hạn tenant (dùng cho SuperAdmin hoặc kiểm tra nội bộ).
    /// </summary>
    Task<User?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Soft delete: đặt status = Inactive thay vì xóa document.
    /// </summary>
    Task<bool> SoftDeleteUserAsync(string userId);

    /// <summary>
    /// Bật/tắt trạng thái Active/Inactive.
    /// </summary>
    Task<bool> ToggleUserActiveAsync(string userId);

    /// <summary>
    /// Thay đổi role (chỉ SuperAdmin).
    /// </summary>
    Task<bool> UpdateUserRoleAsync(string userId, string roleId);

    /// <summary>
    /// Chuyển tenant (chỉ SuperAdmin).
    /// </summary>
    Task<bool> UpdateUserTenantAsync(string userId, string tenantId);
}
