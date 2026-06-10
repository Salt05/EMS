namespace EMS.Shared.DTOs.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? MSSV { get; set; }
    public string? PhoneNumber { get; set; }
}
