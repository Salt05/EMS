namespace EMS.Shared.DTOs;

public class UserDTO
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? MSSV { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
