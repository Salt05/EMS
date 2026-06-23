using EMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAdminUserService _adminUserService;

    public TestController(IUserService userService, IAdminUserService adminUserService)
    {
        _userService = userService;
        _adminUserService = adminUserService;
    }

    [HttpGet("fix-admin")]
    public async Task<IActionResult> FixAdmin()
    {
        // Find admin@ems.com
        var user = await _userService.GetUserByEmailAsync("admin@ems.com", "default");
        if (user == null)
            user = await _userService.GetUserByEmailAsync("admin@ems.com", "ems");
            
        if (user == null)
            return Ok("Không tìm thấy admin@ems.com");

        var success = await _adminUserService.UpdateUserRoleAsync(user.Id, "superadmin");
        if (success)
            return Ok($"Đã khôi phục quyền Super Admin cho {user.Email} thành công!");
        
        return Ok("Có lỗi xảy ra khi cập nhật!");
    }
}
