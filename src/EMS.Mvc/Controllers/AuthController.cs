using Microsoft.AspNetCore.Mvc;
using EMS.Shared.DTOs.Auth;

namespace EMS.Mvc.Controllers;

public class AuthController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        // Nếu đã đăng nhập, chuyển hướng về Home
        if (Request.Cookies.ContainsKey("user_session"))
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(string.Empty, "Email và mật khẩu không được để trống.");
            return View(request);
        }

        // Mock check (chấp nhận bất kỳ thông tin nào hợp lệ để test)
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

        // Giả lập lưu session bằng Cookie
        string fullName = request.Email.Split('@')[0];
        fullName = char.ToUpper(fullName[0]) + fullName.Substring(1); // Viết hoa chữ cái đầu
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddHours(1)
        };
        
        Response.Cookies.Append("user_session", $"{fullName}|{request.Email}|Student", cookieOptions);
        TempData["SuccessMessage"] = "Đăng nhập thành công! Chào mừng bạn quay trở lại.";

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
    public IActionResult Register(RegisterRequest request)
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

        // Đăng ký thành công giả lập
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddHours(1)
        };
        
        Response.Cookies.Append("user_session", $"{request.FullName}|{request.Email}|Student", cookieOptions);
        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công!";

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
