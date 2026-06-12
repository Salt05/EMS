using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.Events;

public class RejectEventDto
{
    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}
