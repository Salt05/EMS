using System.Security.Claims;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Admin;
using EMS.Shared.DTOs.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EMS.WebAPI.Controllers;

/// <summary>
/// SuperAdmin API endpoints cho quản lý Tenants, Users toàn hệ thống, và Events toàn hệ thống.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "superadmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAdminUserService _adminUserService;
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IEventService _eventService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        ITenantService tenantService,
        IAdminUserService adminUserService,
        IUserService userService,
        IAuthService authService,
        IEventService eventService,
        IMemoryCache cache,
        ILogger<SuperAdminController> logger)
    {
        _tenantService = tenantService;
        _adminUserService = adminUserService;
        _userService = userService;
        _authService = authService;
        _eventService = eventService;
        _cache = cache;
        _logger = logger;
    }

    // ==========================================
    // 1. TENANTS MANAGEMENT
    // ==========================================

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants([FromQuery] bool bypassCache = false)
    {
        try
        {
            if (!bypassCache && _cache.TryGetValue<List<TenantDTO>>("superadmin_tenants_dtos", out var cachedList))
            {
                return Ok(cachedList);
            }

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

            var orderedDtos = dtos.OrderBy(d => d.Name).ToList();
            _cache.Set("superadmin_tenants_dtos", orderedDtos, TimeSpan.FromSeconds(30));
            return Ok(orderedDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants list in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] TenantDTO dto)
    {
        try
        {
            var id = string.IsNullOrWhiteSpace(dto.Id)
                ? (string.IsNullOrWhiteSpace(dto.Subdomain) ? Guid.NewGuid().ToString() : $"{dto.Subdomain.ToLowerInvariant()}-tenant")
                : dto.Id;

            var tenant = new Tenant
            {
                Id = id,
                Name = dto.Name,
                Subdomain = dto.Subdomain,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _tenantService.CreateTenantAsync(tenant);
            if (created == null) return StatusCode(500, "Failed to create tenant");

            ClearSuperAdminCaches();
            dto.Id = created.Id;
            dto.CreatedAt = created.CreatedAt;
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("tenants/{id}")]
    public async Task<IActionResult> UpdateTenant(string id, [FromBody] TenantDTO dto)
    {
        try
        {
            var existing = await _tenantService.GetTenantByIdAsync(id);
            if (existing == null) return NotFound("Tenant not found");

            existing.Name = dto.Name;
            existing.Subdomain = dto.Subdomain;
            existing.Email = dto.Email;
            existing.PhoneNumber = dto.PhoneNumber ?? string.Empty;
            existing.Address = dto.Address ?? string.Empty;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            var success = await _tenantService.UpdateTenantAsync(existing);
            if (!success) return StatusCode(500, "Failed to update tenant");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {Id} in SuperAdmin", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> DeleteTenant(string id)
    {
        try
        {
            var success = await _tenantService.DeleteTenantAsync(id);
            if (!success) return StatusCode(500, "Failed to delete tenant");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {Id} in SuperAdmin", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ==========================================
    // 2. USERS MANAGEMENT (ALL TENANTS)
    // ==========================================

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] bool bypassCache = false)
    {
        try
        {
            if (!bypassCache && _cache.TryGetValue<List<AdminUserItemDto>>("superadmin_users_dtos", out var cachedList))
            {
                return Ok(cachedList);
            }

            var (users, _) = await _adminUserService.GetUsersAsync(null, null, null, null, 1, 10000);
            var tenants = await _tenantService.GetTenantsAsync();
            var tenantMap = tenants.ToDictionary(t => t.Id, t => t.Name);

            var list = users.Select(u => new AdminUserItemDto
            {
                Id = u.Id,
                MSSV = u.MSSV,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Department = u.Department,
                RoleIds = u.RoleIds,
                TenantId = u.TenantId,
                TenantName = tenantMap.TryGetValue(u.TenantId, out var name) ? name : u.TenantId,
                Status = (int)u.Status,
                StatusName = u.Status.ToString(),
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).OrderByDescending(u => u.CreatedAt).ToList();

            _cache.Set("superadmin_users_dtos", list, TimeSpan.FromSeconds(30));
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequestDto dto)
    {
        try
        {
            var tenantId = dto.TenantId ?? "default-tenant";
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email, tenantId);
            if (existingUser != null) return BadRequest("User with this email already exists in this tenant");

            var (success, firebaseUid, error) = await _authService.RegisterAsync(dto.Email, dto.Password);
            if (!success) return BadRequest($"Failed to create user: {error}");

            var user = new User
            {
                FirebaseUid = firebaseUid!,
                Email = dto.Email,
                FullName = dto.FullName,
                MSSV = dto.MSSV ?? string.Empty,
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                Department = dto.Department ?? string.Empty,
                TenantId = tenantId,
                RoleIds = new List<string> { dto.RoleId },
                Status = UserStatus.Active
            };

            var created = await _userService.CreateUserAsync(user);
            if (created == null) return StatusCode(500, "Failed to save user to database");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> UpdateUserRole(string id, [FromBody] AdminUpdateRoleRequestDto dto)
    {
        try
        {
            var success = await _adminUserService.UpdateUserRoleAsync(id, dto.RoleId);
            if (!success) return NotFound("User not found or update failed");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("users/{id}/tenant")]
    public async Task<IActionResult> UpdateUserTenant(string id, [FromBody] AdminChangeTenantRequestDto dto)
    {
        try
        {
            var success = await _adminUserService.UpdateUserTenantAsync(id, dto.TenantId);
            if (!success) return NotFound("User not found or update failed");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user tenant in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleUserActive(string id)
    {
        try
        {
            var success = await _adminUserService.ToggleUserActiveAsync(id);
            if (!success) return NotFound("User not found or toggle failed");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var success = await _adminUserService.SoftDeleteUserAsync(id);
            if (!success) return NotFound("User not found or delete failed");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    // ==========================================
    // 3. EVENTS MANAGEMENT (ALL TENANTS)
    // ==========================================

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] bool bypassCache = false)
    {
        try
        {
            if (!bypassCache && _cache.TryGetValue<List<EventResponseDto>>("superadmin_events_dtos", out var cachedList))
            {
                return Ok(cachedList);
            }

            var events = await _eventService.GetEventsByTenantAsync("all");
            var list = events.Select(ev => new EventResponseDto
            {
                Id = ev.Id,
                TenantId = ev.TenantId,
                Title = ev.Title,
                Description = ev.Description,
                Location = ev.Location,
                VenueId = ev.VenueId,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Capacity = ev.Capacity,
                ImageUrl = ev.ImageUrl,
                OrganizerId = ev.OrganizerId,
                Status = (int)ev.Status,
                StatusName = ev.Status.ToString(),
                ApprovedById = ev.ApprovedById,
                ApprovedAt = ev.ApprovedAt,
                RejectionReason = ev.RejectionReason,
                CheckInCode = ev.CheckInCode,
                CheckInCodeExpiresAt = ev.CheckInCodeExpiresAt,
                CreatedAt = ev.CreatedAt,
                UpdatedAt = ev.UpdatedAt
            }).OrderByDescending(e => e.StartTime).ToList();

            _cache.Set("superadmin_events_dtos", list, TimeSpan.FromSeconds(30));
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting events in SuperAdmin");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("events/{id}/cancel")]
    public async Task<IActionResult> CancelEvent(string id, [FromBody] RejectEventDto dto)
    {
        try
        {
            var allEvents = await _eventService.GetEventsByTenantAsync("all");
            var ev = allEvents.FirstOrDefault(e => e.Id == id);
            if (ev == null) return NotFound("Event not found");

            ev.Status = EventStatus.Cancelled;
            ev.RejectionReason = dto.Reason ?? "Cancelled by SuperAdmin";
            ev.UpdatedAt = DateTime.UtcNow;

            var success = await _eventService.UpdateEventAsync(ev);
            if (!success) return StatusCode(500, "Failed to cancel event");

            ClearSuperAdminCaches();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {Id} in SuperAdmin", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private void ClearSuperAdminCaches()
    {
        _cache.Remove("superadmin_tenants_dtos");
        _cache.Remove("superadmin_users_dtos");
        _cache.Remove("superadmin_events_dtos");
    }
}
