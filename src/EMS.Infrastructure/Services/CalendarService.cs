using System;
using System.Text;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace EMS.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    public byte[] GenerateEventIcs(Event ev)
    {
        if (ev == null) throw new ArgumentNullException(nameof(ev));

        var calendar = new Calendar();
        calendar.Method = "PUBLISH";

        var calendarEvent = new CalendarEvent
        {
            Summary = ev.Title,
            Description = ev.Description,
            Location = ev.Location,
            Start = new CalDateTime(ev.StartTime.ToUniversalTime()),
            End = new CalDateTime(ev.EndTime.ToUniversalTime()),
            Uid = $"{ev.Id}@ems.com",
            DtStamp = new CalDateTime(DateTime.UtcNow)
        };

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        var icsString = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(icsString ?? string.Empty);
    }
}
