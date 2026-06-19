using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.CheckIns;

public class GenerateCheckInDto
{
    [Required]
    public string EventId { get; set; } = string.Empty;
}
