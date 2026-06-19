using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.Registrations;

public class RejectRegistrationDto
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
