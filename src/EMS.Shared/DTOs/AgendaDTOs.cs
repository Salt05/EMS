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
}

public class CreateAgendaDto
{
    [Required(ErrorMessage = "Nội dung tiết mục không được để trống.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thời gian bắt đầu không được để trống.")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc không được để trống.")]
    public DateTime EndTime { get; set; }
}

public class UpdateAgendaDto : CreateAgendaDto
{
}
