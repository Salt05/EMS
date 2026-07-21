using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Core.Entities;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
