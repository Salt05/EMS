namespace EMS.Shared.DTOs.CheckIns;

public class CheckInResponseDto
{
    public string RegistrationId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? CheckInCode { get; set; }
    public DateTime? CheckInCodeExpiresAt { get; set; }
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
