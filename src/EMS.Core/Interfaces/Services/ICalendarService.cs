using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

public interface ICalendarService
{
    byte[] GenerateEventIcs(Event ev);
}
