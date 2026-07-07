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
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace EMS.BlazorWASM.Services;

public interface IOrganizerServiceClient
{
    Task<OrganizerDashboardStatsDto> GetStatsAsync(bool bypassCache = false);
    Task<List<EventResponseDto>> GetEventsAsync(bool bypassCache = false);
    Task<List<RegistrationResponseDto>> GetRegistrationsAsync(string eventId, bool bypassCache = false);
    Task<bool> ApproveRegistrationAsync(string regId);
    Task<bool> RejectRegistrationAsync(string regId, string reason);
    Task<string> GenerateCheckInCodeAsync(string eventId, int durationMinutes = 30);
    Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId, bool bypassCache = false);
    Task<bool> CreateEventAsync(CreateEventDto request);
    Task<bool> UpdateEventAsync(string id, UpdateEventDto request);
    Task<bool> DeleteEventAsync(string id);
    Task<byte[]> ExportEventReportAsync(string eventId, string format);
    void InvalidateCache();
}

public class OrganizerServiceClient : IOrganizerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizerServiceClient> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;

    // Client-side Memory Cache
    private OrganizerDashboardStatsDto? _statsCache;
    private List<EventResponseDto>? _eventsCache;
    private readonly Dictionary<string, List<RegistrationResponseDto>> _registrationsCache = new();
    private readonly Dictionary<string, List<RegistrationResponseDto>> _attendeesCache = new();

    public OrganizerServiceClient(HttpClient httpClient, ILogger<OrganizerServiceClient> logger, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    private void ClearCaches()
    {
        _statsCache = null;
        _eventsCache = null;
        _registrationsCache.Clear();
        _attendeesCache.Clear();
    }

    public async Task<OrganizerDashboardStatsDto> GetStatsAsync(bool bypassCache = false)
    {
        if (!bypassCache && _statsCache != null)
        {
            return _statsCache;
        }

        try
        {
            var res = await _httpClient.GetFromJsonAsync<OrganizerDashboardStatsDto>("api/organizer/dashboard/stats");
            _statsCache = res ?? DashboardStatsMock.OrganizerStats;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/organizer/dashboard/stats chưa có, đang dùng mock data.");
            _statsCache = DashboardStatsMock.OrganizerStats;
        }
        return _statsCache;
    }

    public async Task<List<EventResponseDto>> GetEventsAsync(bool bypassCache = false)
    {
        if (!bypassCache && _eventsCache != null)
        {
            return _eventsCache;
        }

        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>("api/events");
            if (res != null)
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    _eventsCache = res.Where(e => e.OrganizerId == currentUserId).ToList();
                }
                else
                {
                    _eventsCache = res;
                }
            }
            else
            {
                _eventsCache = new List<EventResponseDto>();
            }
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/events chưa có, đang dùng mock data.");
            _eventsCache = EventsMock.Events.Where(e => e.OrganizerId == "user2").ToList();
        }
        return _eventsCache;
    }

    public async Task<List<RegistrationResponseDto>> GetRegistrationsAsync(string eventId, bool bypassCache = false)
    {
        if (!bypassCache && _registrationsCache.TryGetValue(eventId, out var cachedRegs))
        {
            return cachedRegs;
        }

        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/registrations/event/{eventId}");
            var list = res ?? RegistrationsMock.Registrations.Where(r => r.EventId == eventId).ToList();
            _registrationsCache[eventId] = list;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API /api/registrations/event/{eventId} chưa có, đang dùng mock data.");
            var list = RegistrationsMock.Registrations.Where(r => r.EventId == eventId).ToList();
            _registrationsCache[eventId] = list;
        }
        return _registrationsCache[eventId];
    }

    public async Task<bool> ApproveRegistrationAsync(string regId)
    {
        ClearCaches();
        try
        {
            var res = await _httpClient.PostAsync($"api/registrations/{regId}/approve", null);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API POST /api/registrations/{regId}/approve chưa có, đang duyệt trên mock data.");
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
        ClearCaches();
        try
        {
            var res = await _httpClient.PostAsJsonAsync($"api/registrations/{regId}/reject", new RejectRegistrationDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API POST /api/registrations/{regId}/reject chưa có, đang từ chối trên mock data.");
        }

        var reg = RegistrationsMock.Registrations.Find(r => r.Id == regId);
        if (reg != null)
        {
            reg.Status = 5; // Rejected (matches RegistrationStatus enum value 5)
            reg.StatusName = "Rejected";
            reg.RejectionReason = reason;
            return true;
        }
        return false;
    }

    public async Task<string> GenerateCheckInCodeAsync(string eventId, int durationMinutes = 30)
    {
        _eventsCache = null; // Clear events cache so the new check-in code updates in the UI
        try
        {
            var res = await _httpClient.PostAsync($"api/events/{eventId}/generate-code?durationMinutes={durationMinutes}", null);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[DEMO] API POST /api/events/{eventId}/generate-code lỗi hoặc chưa có, đang sinh mã trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == eventId);
        if (ev != null)
        {
            ev.CheckInCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            ev.CheckInCodeExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);
            return ev.CheckInCode;
        }
        return string.Empty;
    }

    public async Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId, bool bypassCache = false)
    {
        if (!bypassCache && _attendeesCache.TryGetValue(eventId, out var cachedAttendees))
        {
            return cachedAttendees;
        }

        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/checkin/event/{eventId}/attendees");
            var list = res ?? RegistrationsMock.Registrations.Where(r => r.EventId == eventId && r.CheckedIn).ToList();
            _attendeesCache[eventId] = list;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API /api/checkin/event/{eventId}/attendees chưa có, đang dùng mock data.");
            var list = RegistrationsMock.Registrations.Where(r => r.EventId == eventId && r.CheckedIn).ToList();
            _attendeesCache[eventId] = list;
        }
        return _attendeesCache[eventId];
    }

    public async Task<bool> CreateEventAsync(CreateEventDto request)
    {
        ClearCaches();
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/events", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API POST /api/events chưa có, đang thêm vào mock data.");
        }

        var ev = new EventResponseDto
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            TenantId = "huflit",
            Title = request.Title,
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
        ClearCaches();
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/events/{id}", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/events/{id} chưa có, đang cập nhật mock data.");
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
        ClearCaches();
        try
        {
            var res = await _httpClient.DeleteAsync($"api/events/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API DELETE /api/events/{id} chưa có, đang xóa trên mock data.");
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

    public void InvalidateCache()
    {
        ClearCaches();
    }
}
