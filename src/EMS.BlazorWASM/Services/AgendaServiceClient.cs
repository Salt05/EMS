using System.Net.Http.Json;
using EMS.Shared.DTOs;

namespace EMS.BlazorWASM.Services;

public class AgendaServiceClient : IAgendaServiceClient
{
    private readonly HttpClient _httpClient;

    public AgendaServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AgendaItemDto>> GetAgendaByEventAsync(string eventId)
    {
        return await _httpClient.GetFromJsonAsync<List<AgendaItemDto>>($"/api/events/{eventId}/agenda") 
               ?? new List<AgendaItemDto>();
    }

    public async Task<AgendaItemDto?> CreateAgendaItemAsync(string eventId, CreateAgendaDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/events/{eventId}/agenda", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AgendaItemDto>();
        }
        return null;
    }

    public async Task<AgendaItemDto?> UpdateAgendaItemAsync(string id, UpdateAgendaDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/agenda/{id}", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AgendaItemDto>();
        }
        return null;
    }

    public async Task<bool> DeleteAgendaItemAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"/api/agenda/{id}");
        return response.IsSuccessStatusCode;
    }
}
