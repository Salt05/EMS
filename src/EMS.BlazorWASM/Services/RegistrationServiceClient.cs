using System.Net.Http.Json;
using EMS.Shared.DTOs.Registrations;

namespace EMS.BlazorWASM.Services;

public class RegistrationServiceClient : IRegistrationServiceClient
{
    private readonly HttpClient _httpClient;

    public RegistrationServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RegistrationResponseDto>> GetEventRegistrationsAsync(string eventId, int? status = null)
    {
        var url = status.HasValue ? $"/api/registrations/event/{eventId}?status={status.Value}" : $"/api/registrations/event/{eventId}";
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>(url) ?? new List<RegistrationResponseDto>();
        }
        catch (Exception)
        {
            return new List<RegistrationResponseDto>();
        }
    }

    public async Task<bool> ApproveRegistrationAsync(string registrationId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/registrations/{registrationId}/approve", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> RejectRegistrationAsync(string registrationId, string reason)
    {
        try
        {
            var dto = new RejectRegistrationDto { Reason = reason };
            var response = await _httpClient.PostAsJsonAsync($"/api/registrations/{registrationId}/reject", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<RegistrationResponseDto>> GetMyRegistrationsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>("/api/registrations/me") ?? new List<RegistrationResponseDto>();
        }
        catch (Exception)
        {
            return new List<RegistrationResponseDto>();
        }
    }

    public async Task<RegistrationResponseDto?> RegisterAsync(CreateRegistrationDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/registrations", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RegistrationResponseDto>();
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> CancelRegistrationAsync(string registrationId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/registrations/{registrationId}/cancel", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
