using EMS.Shared.DTOs.Auth;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Interface cho authentication service phía Blazor WASM client.
/// Quản lý login, logout và truy vấn token hiện tại.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng nhập bằng email và password, gọi WebAPI.
    /// </summary>
    /// <param name="request">Thông tin đăng nhập</param>
    /// <returns>LoginResponse nếu thành công, null nếu thất bại</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Đăng xuất, xóa token khỏi localStorage.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Đăng ký tài khoản mới, gọi WebAPI.
    /// </summary>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>Bộ kết quả gồm trạng thái thành công và lỗi (nếu có)</returns>
    Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Lấy JWT token hiện tại từ localStorage.
    /// </summary>
    /// <returns>Token string hoặc null nếu chưa đăng nhập</returns>
    Task<string?> GetCurrentTokenAsync();
}
