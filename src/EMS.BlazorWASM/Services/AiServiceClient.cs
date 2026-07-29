using System.Net.Http.Json;
using EMS.Shared.DTOs;

namespace EMS.BlazorWASM.Services;

public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;

    public AiServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AiGeneratedEventDto?> GenerateEventContentAsync(AiGenerateEventRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/ai/generate-event", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AiGeneratedEventDto>();
        }
        return null;
    }

    public async Task<AiChatResponseDto?> ChatAsync(AiChatRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/ai/chat", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AiChatResponseDto>();
        }
        return null;
    }
}
