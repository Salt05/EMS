namespace EMS.Shared.DTOs.Registrations;

public class RegistrationResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string? ProcessedById { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? UserFullName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserMSSV { get; set; }
    // Check-in tracking
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public string? CheckInCode { get; set; }
    public DateTime? CheckInCodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
