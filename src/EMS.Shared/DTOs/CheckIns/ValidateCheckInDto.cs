using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.CheckIns;

public class ValidateCheckInDto
{
    [Required]
    public string Code { get; set; } = string.Empty;
}
