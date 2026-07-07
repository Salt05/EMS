using System;
using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs;

public class AgendaItemDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Speaker { get; set; } = string.Empty;
    public string? MaterialUrl { get; set; }
    public int Order { get; set; }
}

public class CreateAgendaDto
{
    [Required(ErrorMessage = "Tiêu đề agenda không được để trống.")]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Speaker { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public string? MaterialUrl { get; set; }

    public int Order { get; set; }
}

public class UpdateAgendaDto : CreateAgendaDto
{
}
