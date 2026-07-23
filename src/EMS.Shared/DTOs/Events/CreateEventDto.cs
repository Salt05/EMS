using System.ComponentModel.DataAnnotations;

namespace EMS.Shared.DTOs.Events;

public class CreateEventDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(5000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(300)]
    public string Location { get; set; } = string.Empty;

    public string? VenueId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Range(0, int.MaxValue)]
    public int Capacity { get; set; }

    public string? ImageUrl { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int Scope { get; set; }

    public List<CreateAgendaDto> AgendaItems { get; set; } = new();
}
