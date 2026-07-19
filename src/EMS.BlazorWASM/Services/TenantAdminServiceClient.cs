using EMS.Shared.DTOs;
using EMS.Shared.DTOs.Events;
using EMS.Shared.DTOs.Admin;
using EMS.Shared.DTOs.Registrations;
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
    Task<List<EmailTemplateDto>> GetEmailTemplatesAsync();
    Task<List<EmailTemplateDto>> GetDefaultEmailTemplatesAsync();
    Task<bool> UpdateEmailTemplateAsync(string id, EmailTemplateDto template);
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
            return res ?? new TenantAdminDashboardStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/admin/dashboard/stats.");
            return new TenantAdminDashboardStatsDto();
        }
    }

    public async Task<List<EventResponseDto>> GetEventsAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EventResponseDto>>("api/events");
            return res ?? new List<EventResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API GetEventsAsync.");
            return new List<EventResponseDto>();
        }
    }

    public async Task<bool> ApproveEventAsync(string id)
    {
        try
        {
            var res = await _httpClient.PostAsync($"api/events/{id}/approve", null);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API ApproveEventAsync.");
        }
        return false;
    }

    public async Task<bool> RejectEventAsync(string id, string reason)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync($"api/events/{id}/reject", new RejectEventDto { Reason = reason });
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API RejectEventAsync.");
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
            _logger.LogError(ex, "Lỗi khi gọi API GenerateCheckInCodeAsync.");
            return string.Empty;
        }
    }

    public async Task<List<RegistrationResponseDto>> GetAttendeesAsync(string id)
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<RegistrationResponseDto>>($"api/checkin/event/{id}/attendees");
            return res ?? new List<RegistrationResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API GetAttendeesAsync.");
            return new List<RegistrationResponseDto>();
        }
    }

    public async Task<bool> CreateEventAsync(CreateEventDto request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/events", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API CreateEventAsync.");
        }
        return false;
    }

    public async Task<bool> UpdateEventAsync(string id, UpdateEventDto request)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/events/{id}", request);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API UpdateEventAsync.");
        }
        return false;
    }

    public async Task<bool> DeleteEventAsync(string id)
    {
        try
        {
            var res = await _httpClient.DeleteAsync($"api/events/{id}");
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API DeleteEventAsync.");
        }
        return false;
    }

    public async Task<List<EmailTemplateDto>> GetEmailTemplatesAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EmailTemplateDto>>("api/admin/email-templates");
            return res ?? new List<EmailTemplateDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/admin/email-templates.");
            return new List<EmailTemplateDto>();
        }
    }

    public async Task<List<EmailTemplateDto>> GetDefaultEmailTemplatesAsync()
    {
        try
        {
            var res = await _httpClient.GetFromJsonAsync<List<EmailTemplateDto>>("api/admin/email-templates/defaults");
            return res ?? new List<EmailTemplateDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API /api/admin/email-templates/defaults.");
            return new List<EmailTemplateDto>();
        }
    }

    public async Task<bool> UpdateEmailTemplateAsync(string id, EmailTemplateDto template)
    {
        try
        {
            var res = await _httpClient.PutAsJsonAsync($"api/admin/email-templates/{id}", template);
            if (res.IsSuccessStatusCode) return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi gọi API PUT /api/admin/email-templates/{id}.");
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
