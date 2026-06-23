using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers.Admin;

/// <summary>
/// API quản lý user dành cho Admin Dashboard.
/// SuperAdmin: quản lý toàn bộ hệ thống.
/// TenantAdmin (admin role): quản lý user trong tenant của mình.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "admin,superadmin,Admin,SuperAdmin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IAdminUserService adminUserService,
        IUserService userService,
        IAuthService authService,
        ITenantService tenantService,
        ILogger<AdminUsersController> logger)
    {
        _adminUserService = adminUserService;
        _userService = userService;
        _authService = authService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/admin/users — Danh sách user có phân trang và filter.
    /// SuperAdmin: xem tất cả. TenantAdmin: chỉ xem tenant mình.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUserFilterDto filter)
    {
        try
        {
            // Determine tenant scope
            string? tenantIdFilter = null;
            if (!IsSuperAdmin())
            {
                // TenantAdmin chỉ xem user trong tenant của mình
                tenantIdFilter = GetCurrentTenantId();
            }
            else if (!string.IsNullOrEmpty(filter.TenantId))
            {
                // SuperAdmin có thể filter theo tenant cụ thể
                tenantIdFilter = filter.TenantId;
            }

            var (users, totalCount) = await _adminUserService.GetUsersAsync(
                tenantIdFilter, filter.Search, filter.RoleId, filter.Status,
                filter.Page, filter.PageSize);

            // Lấy danh sách tenants để map tên
            var tenants = await _tenantService.GetTenantsAsync();
            var tenantMap = tenants.ToDictionary(t => t.Id, t => t.Name);

            var response = new AdminUserListResponseDto
            {
                Items = users.Select(u => MapToDto(u, tenantMap)).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin user list");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// GET /api/admin/users/{id} — Chi tiết user.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        try
        {
            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // TenantAdmin chỉ xem user trong tenant mình
            if (!IsSuperAdmin() && user.TenantId != GetCurrentTenantId())
                return Forbid();

            var tenants = await _tenantService.GetTenantsAsync();
            var tenantMap = tenants.ToDictionary(t => t.Id, t => t.Name);

            return Ok(MapToDto(user, tenantMap));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// POST /api/admin/users — Tạo user mới.
    /// Đăng ký Firebase Auth + lưu Firestore.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
                return BadRequest("Email and FullName are required");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Password is required");

            // Xác định tenant
            string tenantId;
            if (IsSuperAdmin())
            {
                tenantId = dto.TenantId ?? GetCurrentTenantId();
            }
            else
            {
                // TenantAdmin: luôn dùng tenant hiện tại
                tenantId = GetCurrentTenantId();

                // TenantAdmin được tạo admin, nhưng không được tạo superadmin
                if (dto.RoleId == "superadmin")
                    return Forbid();
            }

            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("Invalid tenant");

            // Kiểm tra email đã tồn tại chưa
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email, tenantId);
            if (existingUser != null)
                return BadRequest("User with this email already exists in this tenant");

            // Đăng ký Firebase Auth
            var (success, firebaseUid, error) = await _authService.RegisterAsync(dto.Email, dto.Password);
            if (!success)
            {
                _logger.LogError("Firebase registration failed: {Error}", error);
                return BadRequest($"Failed to create user: {error}");
            }

            // Tạo user trong Firestore
            var user = new User
            {
                FirebaseUid = firebaseUid!,
                Email = dto.Email,
                FullName = dto.FullName,
                MSSV = dto.MSSV,
                PhoneNumber = dto.PhoneNumber,
                Department = dto.Department,
                TenantId = tenantId,
                RoleIds = new List<string> { dto.RoleId },
                Status = UserStatus.Active
            };

            var createdUser = await _userService.CreateUserAsync(user);
            if (createdUser == null)
                return StatusCode(500, "Failed to create user in database");

            _logger.LogInformation("Admin created user {Email} in tenant {TenantId}", dto.Email, tenantId);
            return Ok(new { message = "User created successfully", userId = createdUser.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// PUT /api/admin/users/{id} — Cập nhật thông tin user.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUpdateUserRequestDto dto)
    {
        try
        {
            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // TenantAdmin chỉ cập nhật user trong tenant mình
            if (!IsSuperAdmin() && user.TenantId != GetCurrentTenantId())
                return Forbid();

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Department = dto.Department;
            user.UpdatedAt = DateTime.UtcNow;

            var success = await _userService.UpdateUserAsync(user);
            if (!success) return StatusCode(500, "Failed to update user");

            _logger.LogInformation("Admin updated user {UserId}", id);
            return Ok(new { message = "User updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// DELETE /api/admin/users/{id} — Soft delete.
    /// Không cho xóa chính mình.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            if (id == GetCurrentUserId())
                return BadRequest("Cannot delete yourself");

            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // TenantAdmin chỉ xóa user trong tenant mình
            if (!IsSuperAdmin() && user.TenantId != GetCurrentTenantId())
                return Forbid();

            var success = await _adminUserService.SoftDeleteUserAsync(id);
            if (!success) return StatusCode(500, "Failed to delete user");

            _logger.LogInformation("Admin soft-deleted user {UserId}", id);
            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// PUT /api/admin/users/{id}/role — Đổi role.
    /// SuperAdmin có thể set mọi role. TenantAdmin chỉ có thể set employee/manager cho user trong tenant của mình.
    /// </summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "superadmin,SuperAdmin,admin,Admin")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] AdminUpdateRoleRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.RoleId))
                return BadRequest("RoleId is required");

            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // TenantAdmin restrictions
            if (!IsSuperAdmin())
            {
                if (user.TenantId != GetCurrentTenantId())
                    return Forbid();

                // TenantAdmin có thể tạo thêm TenantAdmin khác cùng trường, nhưng không thể tạo SuperAdmin
                if (dto.RoleId == "superadmin")
                    return BadRequest("Tenant Admins cannot grant superadmin roles");
            }

            var success = await _adminUserService.UpdateUserRoleAsync(id, dto.RoleId);
            if (!success) return StatusCode(500, "Failed to change role");

            _logger.LogInformation("Admin changed role of user {UserId} to {RoleId}", id, dto.RoleId);
            return Ok(new { message = "Role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing role for user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// PUT /api/admin/users/{id}/toggle-active — Khóa/mở khóa user.
    /// Không cho khóa chính mình.
    /// </summary>
    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string id)
    {
        try
        {
            if (id == GetCurrentUserId())
                return BadRequest("Cannot toggle your own status");

            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // TenantAdmin chỉ thao tác user trong tenant mình
            if (!IsSuperAdmin() && user.TenantId != GetCurrentTenantId())
                return Forbid();

            var success = await _adminUserService.ToggleUserActiveAsync(id);
            if (!success) return StatusCode(500, "Failed to toggle status");

            _logger.LogInformation("Admin toggled active status of user {UserId}", id);
            return Ok(new { message = "Status toggled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// PUT /api/admin/users/{id}/tenant — Chuyển tenant (chỉ SuperAdmin).
    /// </summary>
    [HttpPut("{id}/tenant")]
    [Authorize(Roles = "superadmin,SuperAdmin")]
    public async Task<IActionResult> ChangeTenant(string id, [FromBody] AdminChangeTenantRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.TenantId))
                return BadRequest("TenantId is required");

            var user = await _adminUserService.GetUserByIdAsync(id);
            if (user == null) return NotFound("User not found");

            // Verify target tenant exists
            var tenant = await _tenantService.GetTenantByIdAsync(dto.TenantId);
            if (tenant == null) return NotFound("Target tenant not found");

            var success = await _adminUserService.UpdateUserTenantAsync(id, dto.TenantId);
            if (!success) return StatusCode(500, "Failed to change tenant");

            _logger.LogInformation("SuperAdmin moved user {UserId} to tenant {TenantId}", id, dto.TenantId);
            return Ok(new { message = "Tenant changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing tenant for user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ============ HELPERS ============

    private bool IsSuperAdmin() =>
        User.IsInRole("superadmin") || User.IsInRole("SuperAdmin");

    private string GetCurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    private string GetCurrentTenantId() =>
        User.FindFirst("tenantId")?.Value ?? string.Empty;

    private static AdminUserItemDto MapToDto(User user, Dictionary<string, string> tenantMap) => new()
    {
        Id = user.Id,
        MSSV = user.MSSV,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Department = user.Department,
        RoleIds = user.RoleIds,
        TenantId = user.TenantId,
        TenantName = tenantMap.TryGetValue(user.TenantId, out var name) ? name : user.TenantId,
        Status = (int)user.Status,
        StatusName = user.Status.ToString(),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
