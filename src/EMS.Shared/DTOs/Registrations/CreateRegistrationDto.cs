using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.Registrations;

public class CreateRegistrationDto
{
    [Required]
    public string EventId { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Note { get; set; }
}
