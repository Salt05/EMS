using System.Collections.Generic;
using System.Threading.Tasks;
using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

public interface IAgendaService
{
    Task<List<AgendaItem>> GetAgendaByEventAsync(string eventId, string tenantId);
    Task<AgendaItem?> GetAgendaItemByIdAsync(string id, string tenantId);
    Task<AgendaItem?> CreateAgendaItemAsync(AgendaItem item);
    Task<bool> UpdateAgendaItemAsync(AgendaItem item);
    Task<bool> DeleteAgendaItemAsync(string id, string tenantId);
}
