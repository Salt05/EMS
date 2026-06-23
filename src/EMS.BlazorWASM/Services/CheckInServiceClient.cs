using System.Net.Http.Json;
using EMS.Shared.DTOs.CheckIns;
using EMS.Shared.DTOs.Registrations;

namespace EMS.BlazorWASM.Services;

public class CheckInServiceClient : ICheckInServiceClient
{
    private readonly HttpClient _httpClient;

    public CheckInServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CheckInResponseDto?> ValidateCheckInCodeAsync(string code)
    {
        var dto = new ValidateCheckInDto { Code = code };
        var response = await _httpClient.PostAsJsonAsync("api/checkin/validate", dto);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CheckInResponseDto>();
        }
        
        // You could handle specific errors here or throw an exception,
        // but for now we'll just return null or let the component handle the failure.
        return null;
    }

    public async Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId)
    {
        var response = await _httpClient.GetAsync($"api/checkin/event/{eventId}/attendees");
        if (response.IsSuccessStatusCode)
        {
            var attendees = await response.Content.ReadFromJsonAsync<List<RegistrationResponseDto>>();
            return attendees ?? new List<RegistrationResponseDto>();
        }
        
        return new List<RegistrationResponseDto>();
    }
}
