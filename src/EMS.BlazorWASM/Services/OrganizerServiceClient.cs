using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Events;
using EMS.Shared.DTOs.Registrations;
using EMS.BlazorWASM.MockData;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EMS.BlazorWASM.Services;

public interface IOrganizerServiceClient
{
    Task<OrganizerDashboardStatsDto> GetStatsAsync();
    Task<List<EventResponseDto>> GetEventsAsync();
    Task<List<RegistrationResponseDto>> GetRegistrationsAsync(string eventId);
    Task<bool> ApproveRegistrationAsync(string regId);
    Task<bool> RejectRegistrationAsync(string regId, string reason);
    Task<string> GenerateCheckInCodeAsync(string eventId);
    Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId);
    Task<bool> CreateEventAsync(CreateEventDto request);
    Task<bool> UpdateEventAsync(string id, UpdateEventDto request);
    Task<bool> DeleteEventAsync(string id);
    Task<byte[]> ExportEventReportAsync(string eventId, string format);
}

public class OrganizerServiceClient : IOrganizerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizerServiceClient> _logger;

    public OrganizerServiceClient(HttpClient httpClient, ILogger<OrganizerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrganizerDashboardStatsDto> GetStatsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<OrganizerDashboardStatsDto>("api/organizer/dashboard/stats");
            return res ?? DashboardStatsMock.OrganizerStats;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/organizer/dashboard/stats chưa có, đang dùng mock data.");
            return DashboardStatsMock.OrganizerStats;
        }
    }

    public async Task<List<EventResponseDto>> GetEventsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>("api/organizer/events");
            return res ?? EventsMock.Events.Where(e => e.OrganizerId == "user2").ToList();
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/organizer/events chưa có, đang dùng mock data.");
            return EventsMock.Events.Where(e => e.OrganizerId == "user2").ToList();
        }
    }

    public async Task<List<RegistrationResponseDto>> GetRegistrationsAsync(string eventId)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/organizer/events/{eventId}/registrations");
            return res ?? RegistrationsMock.Registrations.Where(r => r.EventId == eventId).ToList();
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API /api/organizer/events/{eventId}/registrations chưa có, đang dùng mock data.");
            return RegistrationsMock.Registrations.Where(r => r.EventId == eventId).ToList();
        }
    }

    public async Task<bool> ApproveRegistrationAsync(string regId)
    {
        try
        {
            var res = await _httpClient.PutAsync($"api/organizer/registrations/{regId}/approve", null);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/organizer/registrations/{regId}/approve chưa có, đang duyệt trên mock data.");
        }

        var reg = RegistrationsMock.Registrations.Find(r => r.Id == regId);
        if (reg != null)
        {
            reg.Status = 2; // Confirmed
            reg.StatusName = "Confirmed";
            return true;
        }
        return false;
    }

    public async Task<bool> RejectRegistrationAsync(string regId, string reason)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/organizer/registrations/{regId}/reject", new RejectRegistrationDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/organizer/registrations/{regId}/reject chưa có, đang từ chối trên mock data.");
        }

        var reg = RegistrationsMock.Registrations.Find(r => r.Id == regId);
        if (reg != null)
        {
            reg.Status = 6; // Rejected
            reg.StatusName = "Rejected";
            reg.RejectionReason = reason;
            return true;
        }
        return false;
    }

    public async Task<string> GenerateCheckInCodeAsync(string eventId)
    {
        try
        {
            var res = await _httpClient.PostAsync($"api/organizer/events/{eventId}/generate-code", null);
            if (res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync();
                return content;
            }
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API POST /api/organizer/events/{eventId}/generate-code chưa có, đang sinh mã trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == eventId);
        if (ev != null)
        {
            ev.CheckInCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            ev.CheckInCodeExpiresAt = DateTime.UtcNow.AddMinutes(30);
            return ev.CheckInCode;
        }
        return string.Empty;
    }

    public async Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/organizer/events/{eventId}/attendees");
            return res ?? RegistrationsMock.Registrations.Where(r => r.EventId == eventId && r.CheckedIn).ToList();
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API /api/organizer/events/{eventId}/attendees chưa có, đang dùng mock data.");
            return RegistrationsMock.Registrations.Where(r => r.EventId == eventId && r.CheckedIn).ToList();
        }
    }

    public async Task<bool> CreateEventAsync(CreateEventDto request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/organizer/events", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API POST /api/organizer/events chưa có, đang thêm vào mock data.");
        }

        var ev = new EventResponseDto
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            TenantId = "huflit",
            Title = request.Title + " (DEMO)",
            Description = request.Description,
            Location = request.Location,
            VenueId = request.VenueId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            ImageUrl = request.ImageUrl,
            OrganizerId = "user2",
            Status = 1, // Pending
            StatusName = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        EventsMock.Events.Add(ev);
        return true;
    }

    public async Task<bool> UpdateEventAsync(string id, UpdateEventDto request)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/organizer/events/{id}", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/organizer/events/{id} chưa có, đang cập nhật mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            ev.Title = request.Title;
            ev.Description = request.Description;
            ev.Location = request.Location;
            ev.VenueId = request.VenueId;
            ev.StartTime = request.StartTime;
            ev.EndTime = request.EndTime;
            ev.Capacity = request.Capacity;
            ev.ImageUrl = request.ImageUrl;
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteEventAsync(string id)
    {
        try
        {
            var res = await _httpClient.DeleteAsync($"api/organizer/events/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API DELETE /api/organizer/events/{id} chưa có, đang xóa trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            EventsMock.Events.Remove(ev);
            return true;
        }
        return false;
    }

    public async Task<byte[]> ExportEventReportAsync(string eventId, string format)
    {
        try
        {
            var url = $"api/reports/events/{eventId}/registrations?format={format}";
            return await _httpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting event report for event {EventId} in format {Format}", eventId, format);
            return Array.Empty<byte>();
        }
    }
}
