using EMS.Core.Entities;

namespace EMS.Mvc.Models;

public class UpcomingEventBannerViewModel
{
    public Event Event { get; set; } = default!;
    public Registration Registration { get; set; } = default!;

    public bool IsOngoing { get; set; }
    public bool IsUpcomingWithin24h { get; set; }

    // For Upcoming Countdown (< 24h)
    public TimeSpan TimeUntilStart { get; set; }

    // For Ongoing Live Agenda
    public List<AgendaItem> AgendaItems { get; set; } = new();
    public AgendaItem? CurrentAgendaItem { get; set; }
    public AgendaItem? NextAgendaItem { get; set; }
}
