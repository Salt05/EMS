using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Admin;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Interface cho admin user service phía Blazor WASM client.
/// Gọi WebAPI endpoints /api/admin/users.
/// </summary>
public interface IAdminUserServiceClient
{
    /// <summary>
    /// Lấy danh sách user có phân trang và filter.
    /// </summary>
    Task<AdminUserListResponseDto> GetUsersAsync(AdminUserFilterDto filter);

    /// <summary>
    /// Lấy chi tiết user theo ID.
    /// </summary>
    Task<AdminUserItemDto?> GetUserByIdAsync(string id);

    /// <summary>
    /// Tạo user mới.
    /// </summary>
    Task<(bool Success, string? Error)> CreateUserAsync(AdminCreateUserRequestDto dto);

    /// <summary>
    /// Cập nhật thông tin user.
    /// </summary>
    Task<(bool Success, string? Error)> UpdateUserAsync(string id, AdminUpdateUserRequestDto dto);

    /// <summary>
    /// Soft delete user.
    /// </summary>
    Task<(bool Success, string? Error)> DeleteUserAsync(string id);

    /// <summary>
    /// Đổi role (chỉ SuperAdmin).
    /// </summary>
    Task<(bool Success, string? Error)> ChangeRoleAsync(string id, AdminUpdateRoleRequestDto dto);

    /// <summary>
    /// Khóa/mở khóa user.
    /// </summary>
    Task<(bool Success, string? Error)> ToggleActiveAsync(string id);

    /// <summary>
    /// Chuyển tenant (chỉ SuperAdmin).
    /// </summary>
    Task<(bool Success, string? Error)> ChangeTenantAsync(string id, AdminChangeTenantRequestDto dto);
}
