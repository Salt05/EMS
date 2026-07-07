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
                Title = "Đăng ký & ổn định chỗ ngồi",
                Description = "Đón tiếp sinh viên, quét mã QR check-in nhận tài liệu và ổn định vị trí ngồi tại phòng hội thảo.",
                Speaker = "Ban Tổ Chức",
                Order = 1
            },
            new()
            {
                Id = "agenda-ai-2",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(9).AddMinutes(15),
                EndTime = baseDate.AddHours(10).AddMinutes(15),
                Title = "Ứng dụng AI trong học tập và nghiên cứu khoa học",
                Description = "Giới thiệu tổng quan về các mô hình ngôn ngữ lớn (LLM), hướng dẫn sử dụng ChatGPT, Claude, Gemini hỗ trợ tra cứu, lập dàn ý và dịch thuật.",
                Speaker = "TS. Nguyễn Văn A (Trưởng khoa CNTT)",
                MaterialUrl = "https://example.com/slides/ai-guideline.pdf",
                Order = 2
            },
            new()
            {
                Id = "agenda-ai-3",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(10).AddMinutes(15),
                EndTime = baseDate.AddHours(10).AddMinutes(45),
                Title = "Thực hành Prompt Engineering",
                Description = "Hướng dẫn chi tiết phương pháp viết câu lệnh (prompting) tối ưu cho học tập, tránh các lỗi hallucination và nâng cao độ chính xác.",
                Speaker = "ThS. Trần Thị B (Giảng viên Kỹ thuật Phần mềm)",
                MaterialUrl = "https://example.com/slides/prompt-practical.pdf",
                Order = 3
            },
            new()
            {
                Id = "agenda-ai-4",
                TenantId = tenantId,
                EventId = "evt-workshop-ai",
                StartTime = baseDate.AddHours(10).AddMinutes(45),
                EndTime = baseDate.AddHours(11),
                Title = "Hỏi đáp (Q&A) & Trao chứng nhận",
                Description = "Giải đáp thắc mắc của sinh viên và trao chứng nhận kỹ năng số cho các bạn hoàn thành bài tập thực hành.",
                Speaker = "Diễn giả & Sinh viên",
                Order = 4
            },

            // evt-music-night (Đêm Nhạc Nghệ thuật Sinh viên)
            new()
            {
                Id = "agenda-music-1",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18).AddMinutes(30),
                Title = "Đón khách & Thảm đỏ giao lưu",
                Description = "Đón tiếp sinh viên, chụp ảnh lưu niệm tại khu vực photobooth và nhận quà lưu niệm.",
                Speaker = "Đội Lễ Tân",
                Order = 1
            },
            new()
            {
                Id = "agenda-music-2",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(18).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(19).AddMinutes(30),
                Title = "Acoustic Học Đường: Giai Điệu Tuổi Trẻ",
                Description = "Các tiết mục âm nhạc mộc mạc từ câu lạc bộ văn nghệ sinh viên, tái hiện các ca khúc nổi bật về thời sinh viên.",
                Speaker = "CLB Âm Nhạc EMS",
                Order = 2
            },
            new()
            {
                Id = "agenda-music-3",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(19).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(20).AddMinutes(30),
                Title = "Vũ đạo hiện đại & K-pop Cover",
                Description = "Những màn trình diễn vũ đạo bùng nổ, khuấy động không khí sân khấu ngoài trời.",
                Speaker = "CLB Vũ Đạo DANCE-ALL",
                Order = 3
            },
            new()
            {
                Id = "agenda-music-4",
                TenantId = tenantId,
                EventId = "evt-music-night",
                StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(20).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(21),
                Title = "Bốc thăm trúng thưởng & Kết thúc",
                Description = "Bốc thăm trúng thưởng phần quà may mắn và chụp ảnh lưu niệm tập thể.",
                Speaker = "Ban Tổ Chức",
                Order = 4
            },

            // evt-sports-day (Ngày hội Thể thao EMS Run 2026)
            new()
            {
                Id = "agenda-sports-1",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6).AddMinutes(30),
                Title = "Tập trung & Nhận số Bib chạy",
                Description = "Kiểm tra danh sách đăng ký, quét mã QR check-in và nhận Bib số chạy chính thức.",
                Speaker = "Ban Tổ Chức",
                Order = 1
            },
            new()
            {
                Id = "agenda-sports-2",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(6).AddMinutes(30),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(7),
                Title = "Khởi động tập thể & Khai mạc giải chạy",
                Description = "Các huấn luyện viên hướng dẫn khởi động kỹ thuật tránh chấn thương và BTC phát biểu khai mạc.",
                Speaker = "HLV CLB Thể Thao",
                Order = 2
            },
            new()
            {
                Id = "agenda-sports-3",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(7),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(9),
                Title = "Giải chạy chính thức (Hạng mục 3km & 5km)",
                Description = "Bắt đầu xuất phát giải chạy, bố trí các trạm tiếp nước và y tế dọc tuyến đường chạy.",
                Speaker = "Tất cả Vận Động Viên",
                Order = 3
            },
            new()
            {
                Id = "agenda-sports-4",
                TenantId = tenantId,
                EventId = "evt-sports-day",
                StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(9),
                EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(10),
                Title = "Bế mạc, Trao giải & Vinh danh finisher",
                Description = "Trao giải nhất, nhì, ba cho các hạng mục nam/nữ, trao huy chương hoàn thành cho các vận động viên.",
                Speaker = "Ban Giám Hiệu & BTC",
                Order = 4
            }
        };
    }

    public Task<List<AgendaItem>> GetAgendaByEventAsync(string eventId, string tenantId)
    {
        var list = _agendaItems
            .Where(a => a.EventId == eventId && a.TenantId == tenantId)
            .OrderBy(a => a.Order)
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
