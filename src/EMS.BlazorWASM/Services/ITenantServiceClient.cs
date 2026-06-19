using EMS.Shared.DTOs;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Interface cho tenant service phía Blazor WASM client.
/// Cho phép SuperAdmin chuyển đổi giữa các tenant.
/// </summary>
public interface ITenantServiceClient
{
    /// <summary>
    /// Lấy danh sách tất cả tenants từ WebAPI.
    /// </summary>
    Task<List<TenantDTO>> GetTenantsAsync();

    /// <summary>
    /// Đặt tenant hiện tại (lưu vào localStorage).
    /// </summary>
    /// <param name="tenantId">ID của tenant cần chuyển sang</param>
    Task SetCurrentTenantAsync(string tenantId);

    /// <summary>
    /// Lấy ID tenant hiện tại đang được chọn.
    /// </summary>
    Task<string?> GetCurrentTenantIdAsync();
}
