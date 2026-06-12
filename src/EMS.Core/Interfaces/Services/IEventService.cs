using EMS.Core.Entities;
using EMS.Core.Entities.Enums;

namespace EMS.Core.Interfaces.Services;

public interface IEventService
{
    Task<Event?> GetEventByIdAsync(string eventId, string tenantId);
    Task<List<Event>> GetEventsByTenantAsync(string tenantId, EventStatus? status = null);
    Task<Event?> CreateEventAsync(Event ev);
    Task<bool> UpdateEventAsync(Event ev);
    Task<bool> DeleteEventAsync(string eventId, string tenantId);
    Task<bool> ApproveEventAsync(string eventId, string tenantId, string approvedById);
    Task<bool> RejectEventAsync(string eventId, string tenantId, string approvedById, string reason);
}
