using System.Net.Http.Json;
using EMS.Shared.DTOs.Events;

namespace EMS.BlazorWASM.Services;

public class EventServiceClient : IEventServiceClient
{
    private readonly HttpClient _httpClient;

    public EventServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EventResponseDto>> GetEventsAsync(int? status = null)
    {
        var url = status.HasValue ? $"/api/events?status={status.Value}" : "/api/events";
        return await _httpClient.GetFromJsonAsync<List<EventResponseDto>>(url) ?? new List<EventResponseDto>();
    }

    public async Task<EventResponseDto?> GetEventByIdAsync(string id)
    {
        return await _httpClient.GetFromJsonAsync<EventResponseDto>($"/api/events/{id}");
    }

    public async Task<EventResponseDto?> CreateEventAsync(CreateEventDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/events", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<EventResponseDto>();
        }
        return null;
    }

    public async Task<bool> UpdateEventAsync(string id, UpdateEventDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/events/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEventAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"/api/events/{id}");
        return response.IsSuccessStatusCode;
    }
}
