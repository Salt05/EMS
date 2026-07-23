using System.Net.Http.Json;
using Blazored.LocalStorage;
using EMS.Shared.DTOs.Auth;
using Microsoft.Extensions.Logging;

namespace EMS.BlazorWASM.Services;

/// <summary>
/// Service xử lý authentication cho Blazor WASM client.
/// Gọi WebAPI để login, quản lý token trong localStorage.
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        CustomAuthStateProvider authStateProvider,
        ILocalStorageService localStorage,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
        _logger = logger;
    }

    /// <summary>
    /// Đăng nhập bằng email và password.
    /// Gọi WebAPI POST /api/auth/login, lưu token nếu thành công.
    /// </summary>
    public async Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed with status code: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return (null, string.IsNullOrWhiteSpace(errorContent) ? "Email hoặc mật khẩu không đúng." : errorContent);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
            {
                await _authStateProvider.MarkUserAsAuthenticated(loginResponse.AccessToken);
                await _localStorage.SetItemAsStringAsync("userId", loginResponse.UserId);
                _logger.LogInformation("User {Email} logged in successfully", loginResponse.Email);
            }

            return (loginResponse, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", request.Email);
            return (null, "Đã xảy ra lỗi kết nối với máy chủ.");
        }
    }

    /// <summary>
    /// Đăng xuất, xóa token và thông báo auth state changed.
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _authStateProvider.MarkUserAsLoggedOut();
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userId");
            await _localStorage.RemoveItemAsync("currentTenantId");
            _logger.LogInformation("User logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
        }
    }

    /// <summary>
    /// Đăng ký tài khoản mới, gọi WebAPI.
    /// </summary>
    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registration successful for {Email}", request.Email);
                return (true, null);
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Registration failed: {Error}", errorMsg);
            return (false, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", request.Email);
            return (false, "Đã xảy ra lỗi kết nối với máy chủ.");
        }
    }

    /// <summary>
    /// Lấy JWT token hiện tại từ localStorage.
    /// </summary>
    public async Task<string?> GetCurrentTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            return string.IsNullOrWhiteSpace(token) ? null : token.Trim('"');
        }
        catch
        {
            return null;
        }
    }
}
