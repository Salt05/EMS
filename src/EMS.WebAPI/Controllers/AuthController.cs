using EMS.Core.Interfaces.Services;
using EMS.Core.Entities;
using EMS.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ITenantService _tenantService;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        IJwtService jwtService,
        ITenantService tenantService,
        ITenantResolver tenantResolver,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _jwtService = jwtService;
        _tenantService = tenantService;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required");
            }

            // Get tenant from subdomain
            var subdomain = HttpContext.Items["Subdomain"]?.ToString();
            if (string.IsNullOrEmpty(subdomain))
            {
                return BadRequest("Invalid tenant");
            }

            var tenant = await _tenantService.GetTenantBySubdomainAsync(subdomain);
            if (tenant == null)
            {
                return NotFound("Tenant not found");
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(request.Email, tenant.Id);
            if (existingUser != null)
            {
                return BadRequest("User already exists");
            }

            // Register with Firebase
            var (success, firebaseUid, error) = await _authService.RegisterAsync(request.Email, request.Password);
            if (!success)
            {
                _logger.LogError($"Firebase registration failed: {error}");
                return BadRequest($"Registration failed: {error}");
            }

            // Create user in Firestore
            var user = new User
            {
                FirebaseUid = firebaseUid!,
                Email = request.Email,
                FullName = request.FullName,
                MSSV = request.MSSV,
                PhoneNumber = request.PhoneNumber,
                TenantId = tenant.Id,
                RoleIds = new List<string> { "employee" } // Default role
            };

            var createdUser = await _userService.CreateUserAsync(user);
            if (createdUser == null)
            {
                return BadRequest("Failed to create user");
            }

            _logger.LogInformation($"User registered successfully: {request.Email}");
            return Ok(new { message = "Registration successful", userId = createdUser.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Register: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required");
            }

            // Get tenant from subdomain
            var subdomain = HttpContext.Items["Subdomain"]?.ToString();
            if (string.IsNullOrEmpty(subdomain))
            {
                return BadRequest("Invalid tenant");
            }

            var tenant = await _tenantService.GetTenantBySubdomainAsync(subdomain);
            if (tenant == null)
            {
                return NotFound("Tenant not found");
            }

            // Login with Firebase
            var (success, firebaseToken, error) = await _authService.LoginAsync(request.Email, request.Password);
            if (!success)
            {
                _logger.LogWarning($"Firebase login failed for {request.Email}: {error}");
                return Unauthorized("Invalid credentials");
            }

            // Get user from Firestore
            var user = await _userService.GetUserByEmailAsync(request.Email, tenant.Id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Generate JWT token
            var jwtToken = _jwtService.GenerateToken(user.Id, user.Email, user.FullName, user.RoleIds, tenant.Id);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userService.UpdateUserAsync(user);

            var response = new LoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AccessToken = jwtToken,
                ExpiresIn = 3600,
                Roles = user.RoleIds
            };

            _logger.LogInformation($"User logged in successfully: {request.Email}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in Login: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required");
            }

            var result = await _authService.ResetPasswordAsync(request.Email);
            if (!result)
            {
                return BadRequest("Failed to send reset email");
            }

            return Ok(new { message = "Password reset email sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in ForgotPassword: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}
