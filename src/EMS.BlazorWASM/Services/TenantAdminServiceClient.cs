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

public interface ITenantAdminServiceClient
{
    Task<TenantAdminDashboardStatsDto> GetStatsAsync();
    Task<List<EventResponseDto>> GetEventsAsync();
    Task<bool> ApproveEventAsync(string id);
    Task<bool> RejectEventAsync(string id, string reason);
    Task<string> GenerateCheckInCodeAsync(string id, int durationMinutes = 30);
    Task<List<RegistrationResponseDto>> GetAttendeesAsync(string id);
    Task<bool> CreateEventAsync(CreateEventDto request);
    Task<bool> UpdateEventAsync(string id, UpdateEventDto request);
    Task<bool> DeleteEventAsync(string id);
    Task<List<MockEmailTemplateDto>> GetEmailTemplatesAsync();
    Task<bool> UpdateEmailTemplateAsync(string id, MockEmailTemplateDto template);
    Task<byte[]> ExportReportAsync(string? eventId, string format);
    Task<byte[]> ExportSummaryAsync(string format);
}

public class TenantAdminServiceClient : ITenantAdminServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TenantAdminServiceClient> _logger;

    public TenantAdminServiceClient(HttpClient httpClient, ILogger<TenantAdminServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TenantAdminDashboardStatsDto> GetStatsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<TenantAdminDashboardStatsDto>("api/admin/dashboard/stats");
            return res ?? DashboardStatsMock.TenantAdminStats;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/admin/dashboard/stats chưa có, đang dùng mock data.");
            return DashboardStatsMock.TenantAdminStats;
        }
    }

    public async Task<List<EventResponseDto>> GetEventsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>("api/admin/events");
            return res ?? EventsMock.Events.Where(e => e.TenantId == "huflit").ToList();
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/admin/events chưa có, đang dùng mock data.");
            return EventsMock.Events.Where(e => e.TenantId == "huflit").ToList();
        }
    }

    public async Task<bool> ApproveEventAsync(string id)
    {
        try
        {
            var res = await _httpClient.PostAsync($"api/admin/events/{id}/approve", null);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API POST /api/admin/events/{id}/approve chưa có, đang duyệt trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            ev.Status = 2; // Approved
            ev.StatusName = "Approved";
            return true;
        }
        return false;
    }

    public async Task<bool> RejectEventAsync(string id, string reason)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync($"api/admin/events/{id}/reject", new RejectEventDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API POST /api/admin/events/{id}/reject chưa có, đang từ chối trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            ev.Status = 6; // Rejected
            ev.StatusName = "Rejected";
            ev.RejectionReason = reason;
            return true;
        }
        return false;
    }

    public async Task<string> GenerateCheckInCodeAsync(string id, int durationMinutes = 30)
    {
        try
        {
            var res = await _httpClient.PostAsync($"api/events/{id}/generate-code?durationMinutes={durationMinutes}", null);
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[DEMO] API POST /api/events/{id}/generate-code lỗi hoặc chưa có, đang tạo code trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            ev.CheckInCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            ev.CheckInCodeExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes);
            return ev.CheckInCode;
        }
        return string.Empty;
    }

    public async Task<List<RegistrationResponseDto>> GetAttendeesAsync(string id)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/admin/events/{id}/attendees");
            return res ?? RegistrationsMock.Registrations.Where(r => r.EventId == id && r.CheckedIn).ToList();
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API /api/admin/events/{id}/attendees chưa có, đang dùng mock data.");
            return RegistrationsMock.Registrations.Where(r => r.EventId == id && r.CheckedIn).ToList();
        }
    }

    public async Task<bool> CreateEventAsync(CreateEventDto request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/admin/events", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API POST /api/admin/events chưa có, đang thêm vào mock data.");
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
            var res = await _httpClient.PutAsJsonAsync($"api/admin/events/{id}", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/admin/events/{id} chưa có, đang cập nhật mock data.");
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
            var res = await _httpClient.DeleteAsync($"api/admin/events/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API DELETE /api/admin/events/{id} chưa có, đang xóa trên mock data.");
        }

        var ev = EventsMock.Events.Find(e => e.Id == id);
        if (ev != null)
        {
            EventsMock.Events.Remove(ev);
            return true;
        }
        return false;
    }

    public async Task<List<MockEmailTemplateDto>> GetEmailTemplatesAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<MockEmailTemplateDto>>("api/admin/email-templates");
            return res ?? EmailTemplatesMock.Templates;
        }
        catch
        {
            _logger.LogWarning("[DEMO] API /api/admin/email-templates chưa có, đang dùng mock data.");
            return EmailTemplatesMock.Templates;
        }
    }

    public async Task<bool> UpdateEmailTemplateAsync(string id, MockEmailTemplateDto template)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/admin/email-templates/{id}", template);
            if (res.IsSuccessStatusCode) return true;
        }
        catch
        {
            _logger.LogWarning($"[DEMO] API PUT /api/admin/email-templates/{id} chưa có, đang cập nhật mock data.");
        }

        var idx = EmailTemplatesMock.Templates.FindIndex(t => t.Id == id);
        if (idx >= 0)
        {
            EmailTemplatesMock.Templates[idx] = template;
            return true;
        }
        return false;
    }

    public async Task<byte[]> ExportReportAsync(string? eventId, string format)
    {
        try
        {
            var url = string.IsNullOrEmpty(eventId)
                ? $"api/reports/events/summary?format={format}"
                : $"api/reports/events/{eventId}/registrations?format={format}";
            return await _httpClient.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report in format {Format}", format);
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> ExportSummaryAsync(string format)
    {
        return await ExportReportAsync(null, format);
    }
}
