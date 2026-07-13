using Microsoft.AspNetCore.Mvc;
using EMS.Shared.DTOs.Auth;
using EMS.Mvc.Services;
using EMS.Core.Interfaces.Services;
using EMS.Core.Entities;

namespace EMS.Mvc.Controllers;

public class AuthController : Controller
{
    private readonly ITenantService _tenantService;

    public AuthController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Request.Cookies.ContainsKey("user_session"))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(string.Empty, "Email và mật khẩu không được để trống.");
            return View(request);
        }

        if (!request.Email.Contains("@"))
        {
            ModelState.AddModelError(string.Empty, "Email không hợp lệ.");
            return View(request);
        }

        if (request.Password.Length < 6)
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu phải từ 6 ký tự trở lên.");
            return View(request);
        }

        // Tự động nhận diện tenant từ email domain (mọi email đều được xử lý và phân vùng tự động)
        var resolvedTenantId = DevInMemoryTenantService.ResolveTenantIdFromEmail(request.Email)
            ?? DevInMemoryTenantService.HuflitTenantId;

        // Lưu session: fullName|email|role|tenantId
        string fullName = request.Email.Split('@')[0];
        fullName = char.ToUpper(fullName[0]) + fullName.Substring(1);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires  = DateTime.UtcNow.AddHours(8)
        };

        Response.Cookies.Append("user_session", $"{fullName}|{request.Email}|Student|{resolvedTenantId}", cookieOptions);

        var tenant = await _tenantService.GetTenantByIdAsync(resolvedTenantId);
        var tenantName = tenant?.Name ?? resolvedTenantId;
        TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng sinh viên {tenantName}.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (Request.Cookies.ContainsKey("user_session"))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ các trường bắt buộc.");
            return View(request);
        }

        if (request.Password.Length < 6)
        {
            ModelState.AddModelError(string.Empty, "Mật khẩu phải từ 6 ký tự trở lên.");
            return View(request);
        }

        // Tự động nhận diện tenant từ email domain
        var resolvedTenantId = DevInMemoryTenantService.ResolveTenantIdFromEmail(request.Email)
            ?? DevInMemoryTenantService.HuflitTenantId;

        // Lưu session: fullName|email|role|tenantId
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires  = DateTime.UtcNow.AddHours(8)
        };

        Response.Cookies.Append("user_session", $"{request.FullName}|{request.Email}|Student|{resolvedTenantId}", cookieOptions);

        var tenant = await _tenantService.GetTenantByIdAsync(resolvedTenantId);
        var tenantName = tenant?.Name ?? resolvedTenantId;
        TempData["SuccessMessage"] = $"Đăng ký tài khoản thành công! Chào mừng sinh viên {tenantName}.";

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("user_session");
        TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }
}
