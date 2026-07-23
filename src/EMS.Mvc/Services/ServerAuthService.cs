using System.Security.Claims;
using EMS.BlazorWASM.Services;
using EMS.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace EMS.Mvc.Services;

public class ServerAuthService : IAuthService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContext _userContext;

    public ServerAuthService(IHttpContextAccessor httpContextAccessor, IUserContext userContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
    }

    public Task<(LoginResponse? Response, string? ErrorMessage)> LoginAsync(LoginRequest request)
    {
        return Task.FromResult<(LoginResponse?, string?)>((null, "Vui lòng đăng nhập qua trang đăng nhập chính."));
    }

    public async Task LogoutAsync()
    {
        _userContext.ClearSession();
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        return Task.FromResult<(bool, string?)>((false, "Vui lòng đăng ký qua trang đăng ký chính."));
    }

    public Task<string?> GetCurrentTokenAsync()
    {
        return Task.FromResult<string?>(null);
    }
}
