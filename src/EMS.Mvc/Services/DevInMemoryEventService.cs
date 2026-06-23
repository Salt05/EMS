using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// Event service với 3 sự kiện mẫu cho môi trường Development (Visual Studio F5).
/// </summary>
public class DevInMemoryEventService : IEventService
{
    private readonly List<Event> _events;

    public DevInMemoryEventService()
    {
        var tenantId = DevInMemoryTenantService.DefaultTenantId;
        var now = DateTime.UtcNow;

        _events = new List<Event>
        {
            new()
            {
                Id = "evt-workshop-ai",
                TenantId = tenantId,
                Title = "Workshop AI cho Sinh viên",
                Description = "Buổi workshop học thuật giới thiệu ứng dụng Trí tuệ nhân tạo trong học tập và nghiên cứu. "
                    + "Sinh viên sẽ được hướng dẫn sử dụng các công cụ AI hỗ trợ viết báo cáo, tóm tắt tài liệu và lập kế hoạch học tập. "
                    + "Không yêu cầu kiến thức lập trình trước.",
                Location = "Phòng A101 - Tòa nhà A, Khu A",
                StartTime = now.AddDays(7).Date.AddHours(9),
                EndTime = now.AddDays(7).Date.AddHours(11),
                Capacity = 80,
                ImageUrl = "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now.AddDays(-2),
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = "evt-music-night",
                TenantId = tenantId,
                Title = "Đêm Nhạc Nghệ thuật Sinh viên",
                Description = "Chương trình biểu diễn âm nhạc và nghệ thuật do các câu lạc bộ sinh viên tổ chức. "
                    + "Không gian giao lưu, kết nối và thể hiện tài năng nghệ thuật. "
                    + "Mọi sinh viên đều được chào đón tham dự và cổ vũ.",
                Location = "Sân khấu ngoài trời - Khu B",
                StartTime = now.AddDays(14).Date.AddHours(18),
                EndTime = now.AddDays(14).Date.AddHours(21),
                Capacity = 200,
                ImageUrl = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now.AddDays(-1),
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = "evt-sports-day",
                TenantId = tenantId,
                Title = "Ngày hội Thể thao EMS Run 2026",
                Description = "Giải chạy bộ cộng đồng dành cho sinh viên và giảng viên. "
                    + "Hạng mục 3km và 5km, có điểm danh QR và cấp giấy chứng nhận tham gia. "
                    + "Mang giày thể thao thoải mái và đăng ký trước để nhận bib số.",
                Location = "Sân vận động trung tâm",
                StartTime = now.AddDays(21).Date.AddHours(6),
                EndTime = now.AddDays(21).Date.AddHours(10),
                Capacity = 150,
                ImageUrl = "https://images.unsplash.com/photo-1461896836934-ffe607ba7331?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now
            },
            new()
            {
                Id = "evt-photography-workshop",
                TenantId = tenantId,
                Title = "Workshop Nhiếp ảnh Đường phố",
                Description = "Khóa học thực hành nhiếp ảnh đường phố dành cho sinh viên yêu thích nghệ thuật hình ảnh. "
                    + "Học cách sử dụng ánh sáng tự nhiên, bố cục ảnh và kể chuyện qua từng khung hình. "
                    + "Mang theo máy ảnh hoặc smartphone, không yêu cầu kinh nghiệm trước.",
                Location = "Phòng Lab B201 - Tòa nhà B",
                StartTime = now.AddDays(10).Date.AddHours(14),
                EndTime = now.AddDays(10).Date.AddHours(17),
                Capacity = 40,
                ImageUrl = "https://images.unsplash.com/photo-1452587925148-ce544e77e70d?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now.AddDays(-1),
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = "evt-hackathon-innovation",
                TenantId = tenantId,
                Title = "Innovation Hackathon 48H",
                Description = "Cuộc thi lập trình marathon 48 giờ liên tục dành cho các nhóm sinh viên từ 3-5 người. "
                    + "Chủ đề: Ứng dụng AI giải quyết vấn đề xã hội. Giải thưởng tổng trị giá 50 triệu đồng. "
                    + "Đăng ký theo nhóm, ban tổ chức cung cấp suất ăn và đồ uống trong suốt thời gian thi.",
                Location = "Hội trường lớn - Tòa nhà C",
                StartTime = now.AddDays(30).Date.AddHours(8),
                EndTime = now.AddDays(32).Date.AddHours(8),
                Capacity = 120,
                ImageUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now.AddDays(-3),
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-3)
            },
            new()
            {
                Id = "evt-volunteer-environment",
                TenantId = tenantId,
                Title = "Ngày hội Tình nguyện Xanh - Bảo vệ môi trường",
                Description = "Hoạt động tình nguyện dọn dẹp và trồng cây xanh tại khuôn viên trường và công viên lân cận. "
                    + "Mỗi sinh viên tham gia sẽ được cộng 5 điểm rèn luyện. "
                    + "Mặc trang phục thoải mái, ban tổ chức cung cấp dụng cụ và nước uống.",
                Location = "Khuôn viên trường & Công viên Tao Đàn",
                StartTime = now.AddDays(5).Date.AddHours(7),
                EndTime = now.AddDays(5).Date.AddHours(11),
                Capacity = 300,
                ImageUrl = "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-user",
                Status = EventStatus.Approved,
                ApprovedById = "admin-user",
                ApprovedAt = now.AddDays(-2),
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-2)
            }
        };
    }

    public Task<Event?> GetEventByIdAsync(string eventId, string tenantId)
    {
        var ev = _events.FirstOrDefault(e => e.Id == eventId && e.TenantId == tenantId);
        return Task.FromResult(ev);
    }

    public Task<List<Event>> GetEventsByTenantAsync(string tenantId, EventStatus? status = null)
    {
        var query = _events.Where(e => e.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        var result = query.OrderByDescending(e => e.StartTime).ToList();
        return Task.FromResult(result);
    }

    public Task<Event?> CreateEventAsync(Event ev) => Task.FromResult<Event?>(null);

    public Task<bool> UpdateEventAsync(Event ev) => Task.FromResult(false);

    public Task<bool> DeleteEventAsync(string eventId, string tenantId) => Task.FromResult(false);

    public Task<bool> ApproveEventAsync(string eventId, string tenantId, string approvedById) => Task.FromResult(false);

    public Task<bool> RejectEventAsync(string eventId, string tenantId, string approvedById, string reason) => Task.FromResult(false);
}
