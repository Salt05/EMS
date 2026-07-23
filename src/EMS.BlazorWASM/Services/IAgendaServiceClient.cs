using EMS.Shared.DTOs;

namespace EMS.BlazorWASM.Services;

public interface IAgendaServiceClient
{
    Task<List<AgendaItemDto>> GetAgendaByEventAsync(string eventId);
    Task<AgendaItemDto?> CreateAgendaItemAsync(string eventId, CreateAgendaDto dto);
    Task<AgendaItemDto?> UpdateAgendaItemAsync(string id, UpdateAgendaDto dto);
    Task<bool> DeleteAgendaItemAsync(string id);
}
