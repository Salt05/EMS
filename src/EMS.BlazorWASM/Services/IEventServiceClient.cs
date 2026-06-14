using EMS.Shared.DTOs.Events;

namespace EMS.BlazorWASM.Services;

public interface IEventServiceClient
{
    Task<List<EventResponseDto>> GetEventsAsync(int? status = null);
    Task<EventResponseDto?> GetEventByIdAsync(string id);
    Task<EventResponseDto?> CreateEventAsync(CreateEventDto dto);
    Task<bool> UpdateEventAsync(string id, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(string id);
}
