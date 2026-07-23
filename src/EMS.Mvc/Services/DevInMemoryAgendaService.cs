using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

public class DevInMemoryAgendaService : IAgendaService
{
    private readonly List<AgendaItem> _agendaItems;

    public DevInMemoryAgendaService()
    {
        var tenantId = DevInMemoryTenantService.DefaultTenantId;
        var baseDate = DateTime.UtcNow.AddDays(7).Date;

        _agendaItems = new List<AgendaItem>
        {
            // evt-workshop-ai (Workshop AI cho Sinh viên)
            new()
            {
                Id = "agenda-ai-1",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(9),
                EndTime = baseDate.AddHours(9).AddMinutes(15),
                Title = "Đăng ký & ổn định chỗ ngồi"
            },
            new()
            {
                Id = "agenda-ai-2",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(9).AddMinutes(15),
                EndTime = baseDate.AddHours(10).AddMinutes(15),
                Title = "Ứng dụng AI trong học tập và nghiên cứu khoa học"
            },
            new()
            {
                Id = "agenda-ai-3",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(10).AddMinutes(15),
                EndTime = baseDate.AddHours(10).AddMinutes(45),
                Title = "Thực hành Prompt Engineering"
            },
            new()
            {
                Id = "agenda-ai-4",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(10).AddMinutes(45),
                EndTime = baseDate.AddHours(11),
                Title = "Hỏi đáp (Q&A) & Trao chứng nhận"
            },

            // evt-music-night (Đêm Nhạc Nghệ thuật Sinh viên)
            new()
            {
                Id = "agenda-music-1",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18).AddMinutes(30),
                Title = "Đón khách & Thảm đỏ giao lưu"
            },
            new()
            {
                Id = "agenda-music-2",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(19).AddMinutes(30),
                Title = "Acoustic Học Đường: Giai Điệu Tuổi Trẻ"
            },
            new()
            {
                Id = "agenda-music-3",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(19).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(20).AddMinutes(30),
                Title = "Vũ đạo hiện đại & K-pop Cover"
            },
            new()
            {
                Id = "agenda-music-4",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(20).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(21),
                Title = "Bốc thăm trúng thưởng & Kết thúc"
            },

            // evt-sports-day (Ngày hội Thể thao EMS Run 2026)
            new()
            {
                Id = "agenda-sports-1",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6).AddMinutes(30),
                Title = "Tập trung & Nhận số Bib chạy"
            },
            new()
            {
                Id = "agenda-sports-2",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(7),
                Title = "Khởi động tập thể & Khai mạc giải chạy"
            },
            new()
            {
                Id = "agenda-sports-3",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(7),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(9),
                Title = "Giải chạy chính thức (Hạng mục 3km & 5km)"
            },
            new()
            {
                Id = "agenda-sports-4",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(9),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(10),
                Title = "Bế mạc, Trao giải & Vinh danh finisher"
            }
        };
    }

    public Task<List<AgendaItem>> GetAgendaByEventAsync(string eventId, string tenantId)
    {
        var list = _agendaItems
            .Where(a => a.EventId == eventId && a.TenantId == tenantId)
            .OrderBy(a => a.StartTime)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<AgendaItem?> GetAgendaItemByIdAsync(string id, string tenantId)
    {
        var item = _agendaItems.FirstOrDefault(a => a.Id == id && a.TenantId == tenantId);
        return Task.FromResult(item);
    }

    public Task<AgendaItem?> CreateAgendaItemAsync(AgendaItem item)
    {
        _agendaItems.Add(item);
        return Task.FromResult<AgendaItem?>(item);
    }

    public Task<bool> UpdateAgendaItemAsync(AgendaItem item)
    {
        var index = _agendaItems.FindIndex(a => a.Id == item.Id && a.TenantId == item.TenantId);
        if (index == -1) return Task.FromResult(false);
        _agendaItems[index] = item;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAgendaItemAsync(string id, string tenantId)
    {
        var item = _agendaItems.FirstOrDefault(a => a.Id == id && a.TenantId == tenantId);
        if (item == null) return Task.FromResult(false);
        _agendaItems.Remove(item);
        return Task.FromResult(true);
    }
}
