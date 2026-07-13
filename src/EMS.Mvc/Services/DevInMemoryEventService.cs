using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// Event service với sự kiện mẫu cho 3 tenant trường: HUFLIT, HCMUTE, HCMUT.
/// </summary>
public class DevInMemoryEventService : IEventService
{
    private static readonly List<Event> _events = new();
    private static readonly object _lock = new();
    private static bool _initialized = false;

    public DevInMemoryEventService()
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                InitializeEvents();
                _initialized = true;
            }
        }
    }

    public static void EnsureEventsForTenant(string tenantId, string tenantName)
    {
        lock (_lock)
        {
            if (_events.Any(e => e.TenantId == tenantId))
            {
                return;
            }

            var now = DateTime.UtcNow;
            _events.Add(new Event
            {
                Id          = $"{tenantId}-evt-welcome",
                TenantId    = tenantId,
                Title       = $"Lễ chào đón tân sinh viên trường {tenantName}",
                Description = $"Chào mừng bạn đến với hệ thống EMS! Đây là sự kiện đầu tiên được khởi tạo tự động dành cho sinh viên {tenantName}. Hãy cùng tham gia và trải nghiệm các hoạt động bổ ích sắp tới.",
                Location    = "Hội trường chính / Trực tuyến",
                StartTime   = now.AddDays(2).Date.AddHours(9),
                EndTime     = now.AddDays(2).Date.AddHours(11.5),
                Capacity    = 500,
                ImageUrl    = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&auto=format&fit=crop&q=60",
                OrganizerId = $"admin-{tenantId}",
                Status      = EventStatus.Approved,
                ApprovedById = $"admin-{tenantId}",
                ApprovedAt  = now,
                CreatedAt   = now,
                UpdatedAt   = now
            });

            _events.Add(new Event
            {
                Id          = $"{tenantId}-evt-seminar",
                TenantId    = tenantId,
                Title       = $"Hội thảo Khoa học & Công nghệ - Sinh viên {tenantName}",
                Description = $"Buổi chia sẻ kiến thức chuyên môn, định hướng nghiên cứu khoa học và kết nối ý tưởng sáng tạo trong sinh viên trường {tenantName}.",
                Location    = "Giảng đường khu trung tâm",
                StartTime   = now.AddDays(7).Date.AddHours(14),
                EndTime     = now.AddDays(7).Date.AddHours(17),
                Capacity    = 150,
                ImageUrl    = "https://images.unsplash.com/photo-1518770660439-4636190af475?w=800&auto=format&fit=crop&q=60",
                OrganizerId = $"admin-{tenantId}",
                Status      = EventStatus.Approved,
                ApprovedById = $"admin-{tenantId}",
                ApprovedAt  = now,
                CreatedAt   = now,
                UpdatedAt   = now
            });

            // 3. Đang diễn ra
            _events.Add(new Event
            {
                Id          = $"{tenantId}-evt-ongoing",
                TenantId    = tenantId,
                Title       = $"Đại hội CLB & Định hướng Học tập {tenantName}",
                Description = $"Sự kiện đang diễn ra trực tiếp ngày hôm nay để chia sẻ phương pháp học tập và đăng ký gia nhập CLB của trường {tenantName}.",
                Location    = "Nhà đa năng",
                StartTime   = now.AddHours(-1),
                EndTime     = now.AddHours(3),
                Capacity    = 300,
                ImageUrl    = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?w=800&auto=format&fit=crop&q=60",
                OrganizerId = $"admin-{tenantId}",
                Status      = EventStatus.Approved,
                ApprovedById = $"admin-{tenantId}",
                ApprovedAt  = now,
                CreatedAt   = now,
                UpdatedAt   = now
            });

            // 4. Đã kết thúc
            _events.Add(new Event
            {
                Id          = $"{tenantId}-evt-ended",
                TenantId    = tenantId,
                Title       = $"Giải chạy Marathon Sinh viên {tenantName} - ĐÃ KẾT THÚC",
                Description = $"Sự kiện chạy bộ đã diễn ra vào tuần trước và kết thúc thành công tốt đẹp.",
                Location    = "Sân vận động chính",
                StartTime   = now.AddDays(-4).Date.AddHours(7),
                EndTime     = now.AddDays(-4).Date.AddHours(11),
                Capacity    = 800,
                ImageUrl    = "https://images.unsplash.com/photo-1461896836934-ffe607ba7331?w=800&auto=format&fit=crop&q=60",
                OrganizerId = $"admin-{tenantId}",
                Status      = EventStatus.Approved,
                ApprovedById = $"admin-{tenantId}",
                ApprovedAt  = now,
                CreatedAt   = now,
                UpdatedAt   = now
            });

            // 5. Đã hủy
            _events.Add(new Event
            {
                Id          = $"{tenantId}-evt-cancelled",
                TenantId    = tenantId,
                Title       = $"Đêm nhạc Acoustic Sinh viên {tenantName} - ĐÃ HỦY",
                Description = $"Sự kiện văn nghệ âm nhạc đã bị hủy bỏ do điều kiện thời tiết xấu mưa bão lớn kéo dài.",
                Location    = "Sân khấu ngoài trời",
                StartTime   = now.AddDays(5),
                EndTime     = now.AddDays(5).AddHours(3),
                Capacity    = 150,
                ImageUrl    = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=800&auto=format&fit=crop&q=60",
                OrganizerId = $"admin-{tenantId}",
                Status      = EventStatus.Cancelled,
                ApprovedById = $"admin-{tenantId}",
                ApprovedAt  = now,
                CreatedAt   = now,
                UpdatedAt   = now
            });
        }
    }

    private void InitializeEvents()
    {
        var now = DateTime.UtcNow;

        // ─── HUFLIT ──────────────────────────────────────────────────────────
        var huflit = DevInMemoryTenantService.HuflitTenantId;

        // ─── HCMUTE ──────────────────────────────────────────────────────────
        var hcmute = DevInMemoryTenantService.HcmuteTenantId;

        // ─── HCMUT ───────────────────────────────────────────────────────────
        var hcmut = DevInMemoryTenantService.HcmutTenantId;

        _events.AddRange(new List<Event>
        {
            // ═══════════════════════════════════════════════════════════════════
            //  HUFLIT — ĐH Ngoại ngữ - Tin học
            // ═══════════════════════════════════════════════════════════════════
            new()
            {
                Id          = "huflit-evt-ai-workshop",
                TenantId    = huflit,
                Title       = "Workshop AI & Ngoại ngữ: ChatGPT trong học ngoại ngữ",
                Description = "Buổi workshop khám phá cách ứng dụng ChatGPT và các công cụ AI để nâng cao kỹ năng ngoại ngữ. "
                            + "Sinh viên sẽ thực hành viết email tiếng Anh, dịch văn bản và luyện hội thoại cùng AI. "
                            + "Không yêu cầu kinh nghiệm lập trình, phù hợp mọi ngành.",
                Location    = "Phòng E201 - Tòa nhà E, Cơ sở HUFLIT",
                StartTime   = now.AddDays(5).Date.AddHours(8).AddMinutes(30),
                EndTime     = now.AddDays(5).Date.AddHours(11).AddMinutes(30),
                Capacity    = 80,
                ImageUrl    = "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-3),
                CreatedAt   = now.AddDays(-7),
                UpdatedAt   = now.AddDays(-3)
            },
            new()
            {
                Id          = "huflit-evt-translation-contest",
                TenantId    = huflit,
                Title       = "Cuộc thi Dịch thuật Tiếng Nhật - HUFLIT 2026",
                Description = "Cuộc thi dịch thuật thường niên dành cho sinh viên khoa Ngoại ngữ. "
                            + "Ba hạng mục: Nhật - Việt, Anh - Việt và đa ngôn ngữ. "
                            + "Giải nhất nhận học bổng trị giá 10 triệu đồng và cơ hội thực tập tại doanh nghiệp đối tác.",
                Location    = "Hội trường lớn - Tầng 1, Tòa nhà A HUFLIT",
                StartTime   = now.AddDays(12).Date.AddHours(7).AddMinutes(30),
                EndTime     = now.AddDays(12).Date.AddHours(11),
                Capacity    = 150,
                ImageUrl    = "https://images.unsplash.com/photo-1457369804613-52c61a468e7d?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-2),
                CreatedAt   = now.AddDays(-6),
                UpdatedAt   = now.AddDays(-2)
            },
            new()
            {
                Id          = "huflit-evt-tech-fair",
                TenantId    = huflit,
                Title       = "Ngày hội Công nghệ HUFLIT TechFest 2026",
                Description = "Triển lãm sản phẩm công nghệ của sinh viên Khoa Công nghệ Thông tin HUFLIT. "
                            + "Trình diễn đồ án, sản phẩm khởi nghiệp, kết nối doanh nghiệp tuyển dụng. "
                            + "Cơ hội nhận học bổng từ các công ty công nghệ hàng đầu.",
                Location    = "Sảnh lớn Tầng G - HUFLIT",
                StartTime   = now.AddDays(20).Date.AddHours(8),
                EndTime     = now.AddDays(20).Date.AddHours(17),
                Capacity    = 400,
                ImageUrl    = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-4),
                CreatedAt   = now.AddDays(-8),
                UpdatedAt   = now.AddDays(-4)
            },
            new()
            {
                Id          = "huflit-evt-music-night",
                TenantId    = huflit,
                Title       = "Đêm Nhạc HUFLIT Acoustic Night 2026",
                Description = "Chương trình âm nhạc acoustic do các CLB sinh viên HUFLIT tổ chức. "
                            + "Không gian thư giãn cuối học kỳ, giao lưu kết bạn và thưởng thức âm nhạc. "
                            + "Vào cửa miễn phí cho sinh viên HUFLIT.",
                Location    = "Sân khấu ngoài trời - Khuôn viên HUFLIT",
                StartTime   = now.AddDays(8).Date.AddHours(18),
                EndTime     = now.AddDays(8).Date.AddHours(21),
                Capacity    = 200,
                ImageUrl    = "https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-1),
                CheckInCode = "HUFLIT26",
                CheckInCodeExpiredAt = now.AddDays(8).Date.AddHours(22),
                CreatedAt   = now.AddDays(-5),
                UpdatedAt   = now.AddDays(-1)
            },
            new()
            {
                Id          = "huflit-evt-ongoing-orientation",
                TenantId    = huflit,
                Title       = "Buổi Tư vấn Học tập & Định hướng Nghề nghiệp HUFLIT",
                Description = "Buổi tư vấn trực tiếp đang diễn ra trong sáng nay với sự tham gia của các chuyên gia từ doanh nghiệp đối tác, "
                            + "giúp sinh viên định hướng nghề nghiệp, chọn ngành học phù hợp và tìm kiếm cơ hội thực tập.",
                Location    = "Phòng hội thảo B304 - Tòa nhà B, HUFLIT",
                StartTime   = now.AddHours(-1),      // Bắt đầu từ 1 giờ trước
                EndTime     = now.AddHours(2.5),     // Kết thúc sau 2.5 giờ nữa
                Capacity    = 120,
                ImageUrl    = "https://images.unsplash.com/photo-1552664730-d307ca884978?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-2),
                CreatedAt   = now.AddDays(-5),
                UpdatedAt   = now.AddDays(-2)
            },
            new()
            {
                Id          = "huflit-evt-ended-english-camp",
                TenantId    = huflit,
                Title       = "English Summer Camp 2026 — HUFLIT",
                Description = "Trại hè tiếng Anh 5 ngày đã kết thúc thành công tốt đẹp với hơn 200 sinh viên tham gia. "
                            + "Sinh viên được trải nghiệm môi trường học tiếng Anh toàn phần, giao lưu với giáo viên nước ngoài và chinh phục thử thách ngôn ngữ.",
                Location    = "Khuôn viên HUFLIT - Cơ sở Phú Nhuận",
                StartTime   = now.AddDays(-8).Date.AddHours(7),    // Bắt đầu 8 ngày trước
                EndTime     = now.AddDays(-3).Date.AddHours(17),   // Kết thúc 3 ngày trước
                Capacity    = 200,
                ImageUrl    = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-15),
                CreatedAt   = now.AddDays(-20),
                UpdatedAt   = now.AddDays(-3)
            },

            // ── HUFLIT: Đã hủy ─────────────────────────────────────────────────
            new()
            {
                Id          = "huflit-evt-cancelled-festival",
                TenantId    = huflit,
                Title       = "Lễ hội Văn hóa Đa ngôn ngữ HUFLIT Fest 2026 [ĐÃ HỦY]",
                Description = "Chương trình lễ hội văn hóa đã bị hủy bỏ do ban tổ chức không đảm bảo đủ điều kiện về địa điểm và nhân sự. "
                            + "Ban Giám hiệu trường xin lỗi vì sự bất tiện này và sẽ tổ chức lại vào học kỳ tiếp theo.",
                Location    = "Sảnh sự kiện chính - HUFLIT",
                StartTime   = now.AddDays(3).Date.AddHours(8),   // Dự kiến 3 ngày tới nhưng đã bị hủy
                EndTime     = now.AddDays(3).Date.AddHours(17),
                Capacity    = 500,
                ImageUrl    = "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-huflit",
                Status      = EventStatus.Cancelled,
                ApprovedById = "admin-huflit",
                ApprovedAt  = now.AddDays(-5),
                CreatedAt   = now.AddDays(-10),
                UpdatedAt   = now.AddDays(-1)
            },

            // ═══════════════════════════════════════════════════════════════════
            //  HCMUTE — ĐH Sư phạm Kỹ thuật TP.HCM
            // ═══════════════════════════════════════════════════════════════════
            new()
            {
                Id          = "hcmute-evt-robotics",
                TenantId    = hcmute,
                Title       = "Cuộc thi Robothon HCMUTE 2026",
                Description = "Giải thi đấu robot thường niên lớn nhất của ĐH Sư phạm Kỹ thuật. "
                            + "Hai hạng mục: Robot tự hành và robot điều khiển từ xa. "
                            + "Giải thưởng tổng trị giá 100 triệu đồng, cơ hội đại diện Việt Nam thi quốc tế.",
                Location    = "Xưởng Thực hành Cơ khí - Khu A, HCMUTE",
                StartTime   = now.AddDays(7).Date.AddHours(7).AddMinutes(30),
                EndTime     = now.AddDays(7).Date.AddHours(17),
                Capacity    = 200,
                ImageUrl    = "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-4),
                CreatedAt   = now.AddDays(-10),
                UpdatedAt   = now.AddDays(-4)
            },
            new()
            {
                Id          = "hcmute-evt-stem-workshop",
                TenantId    = hcmute,
                Title       = "Workshop STEM: Thiết kế Mạch điện với Arduino",
                Description = "Buổi thực hành lập trình vi điều khiển Arduino cơ bản dành cho sinh viên Kỹ thuật. "
                            + "Học cách thiết kế mạch cảm biến, điều khiển động cơ và kết nối IoT. "
                            + "Ban tổ chức cung cấp linh kiện, sinh viên chỉ cần mang laptop.",
                Location    = "Phòng Lab Điện tử - Nhà B, HCMUTE",
                StartTime   = now.AddDays(10).Date.AddHours(13),
                EndTime     = now.AddDays(10).Date.AddHours(17),
                Capacity    = 50,
                ImageUrl    = "https://images.unsplash.com/photo-1518770660439-4636190af475?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-2),
                CreatedAt   = now.AddDays(-5),
                UpdatedAt   = now.AddDays(-2)
            },
            new()
            {
                Id          = "hcmute-evt-sports",
                TenantId    = hcmute,
                Title       = "Đại hội Thể thao Sinh viên HCMUTE 2026",
                Description = "Giải thể thao truyền thống của sinh viên ĐH Sư phạm Kỹ thuật. "
                            + "Các môn: bóng đá, cầu lông, bóng rổ, bơi lội. "
                            + "Đăng ký theo đội khoa, thời gian thi đấu kéo dài 2 tuần.",
                Location    = "Sân vận động HCMUTE - Khu thể thao",
                StartTime   = now.AddDays(14).Date.AddHours(7),
                EndTime     = now.AddDays(14).Date.AddHours(17),
                Capacity    = 500,
                ImageUrl    = "https://images.unsplash.com/photo-1461896836934-ffe607ba7331?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-5),
                CreatedAt   = now.AddDays(-12),
                UpdatedAt   = now.AddDays(-5)
            },
            new()
            {
                Id          = "hcmute-evt-volunteer",
                TenantId    = hcmute,
                Title       = "Mùa hè Xanh HCMUTE 2026 - Tình nguyện Cộng đồng",
                Description = "Chiến dịch tình nguyện sinh viên hằng năm của HCMUTE. "
                            + "Hỗ trợ xây nhà, dạy học và phát triển cơ sở hạ tầng tại vùng khó khăn. "
                            + "Mỗi sinh viên tham gia nhận 20 điểm rèn luyện và chứng nhận hoạt động xã hội.",
                Location    = "Tập kết: Hội trường lớn HCMUTE",
                StartTime   = now.AddDays(30).Date.AddHours(5),
                EndTime     = now.AddDays(30).Date.AddHours(8),
                Capacity    = 300,
                ImageUrl    = "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-6),
                CreatedAt   = now.AddDays(-14),
                UpdatedAt   = now.AddDays(-6)
            },
            new()
            {
                Id          = "hcmute-evt-ongoing-lab-open",
                TenantId    = hcmute,
                Title       = "Ngày Mở cửa Lab Thực hành Kỹ thuật HCMUTE",
                Description = "Sự kiện Open Lab đang diễn ra trong ngày hôm nay, cho phép sinh viên tham quan và trải nghiệm "
                            + "các phòng thí nghiệm Cơ điện tử, Tự động hóa và Điện tử viễn thông.",
                Location    = "Khu Lab Thực hành - Nhà A, HCMUTE",
                StartTime   = now.AddHours(-2),       // 2h trước
                EndTime     = now.AddHours(5),        // 5h sau
                Capacity    = 200,
                ImageUrl    = "https://images.unsplash.com/photo-1581091226825-a6a2a5aee158?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-3),
                CreatedAt   = now.AddDays(-7),
                UpdatedAt   = now.AddDays(-3)
            },

            // ── HCMUTE: Đã kết thúc (5 ngày trước) ───────────────────────────
            new()
            {
                Id          = "hcmute-evt-ended-graduation",
                TenantId    = hcmute,
                Title       = "Lễ Tốt nghiệp Khóa 2022 — HCMUTE",
                Description = "Buổi lễ trao bằng tốt nghiệp cho sinh viên khóa 2022 đã diễn ra thành công vào tuần trước. "
                            + "Hơn 1.500 tân cử nhân và kỹ sư chính thức nhận bằng và bắt đầu hành trình sự nghiệp mới.",
                Location    = "Hội trường lớn Nhà Văn hóa Sinh viên - HCMUTE",
                StartTime   = now.AddDays(-5).Date.AddHours(8),   // Sáng 5 ngày trước
                EndTime     = now.AddDays(-5).Date.AddHours(12),  // Trưa 5 ngày trước
                Capacity    = 2000,
                ImageUrl    = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-20),
                CreatedAt   = now.AddDays(-30),
                UpdatedAt   = now.AddDays(-5)
            },

            // ── HCMUTE: Đã hủy ─────────────────────────────────────────────────
            new()
            {
                Id          = "hcmute-evt-cancelled-concert",
                TenantId    = hcmute,
                Title       = "Đêm Gala Cuối Năm HCMUTE 2026 [ĐÃ HỦY]",
                Description = "Chương trình gala dinner và biểu diễn văn nghệ cuối năm đã bị hủy do phát sinh vấn đề về kinh phí tổ chức. "
                            + "Ban Chấp hành Hội Sinh viên thông báo sẽ hoàn trả toàn bộ phí đặt chỗ trong vòng 7 ngày làm việc.",
                Location    = "Trung tâm Hội nghị Quận 3, TP.HCM",
                StartTime   = now.AddDays(10).Date.AddHours(17.5),  // Dự kiến 10 ngày tới nhưng đã bị hủy
                EndTime     = now.AddDays(10).Date.AddHours(22),
                Capacity    = 400,
                ImageUrl    = "https://images.unsplash.com/photo-1506157786151-b8491531f063?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmute",
                Status      = EventStatus.Cancelled,
                ApprovedById = "admin-hcmute",
                ApprovedAt  = now.AddDays(-8),
                CreatedAt   = now.AddDays(-15),
                UpdatedAt   = now.AddDays(-2)
            },

            // ═══════════════════════════════════════════════════════════════════
            //  HCMUT — ĐH Bách Khoa TP.HCM
            // ═══════════════════════════════════════════════════════════════════
            new()
            {
                Id          = "hcmut-evt-hackathon",
                TenantId    = hcmut,
                Title       = "BKWave Hackathon 2026 — Smart City Challenge",
                Description = "Cuộc thi lập trình 48 giờ marathon do ĐH Bách Khoa tổ chức. "
                            + "Chủ đề: Giải pháp công nghệ cho Thành phố thông minh. "
                            + "Giải thưởng tổng 200 triệu đồng, có sự tham gia của 20 doanh nghiệp công nghệ hàng đầu.",
                Location    = "Hội trường B4 - Khu B, Bách Khoa TP.HCM",
                StartTime   = now.AddDays(21).Date.AddHours(8),
                EndTime     = now.AddDays(23).Date.AddHours(8),
                Capacity    = 150,
                ImageUrl    = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-7),
                CreatedAt   = now.AddDays(-14),
                UpdatedAt   = now.AddDays(-7)
            },
            new()
            {
                Id          = "hcmut-evt-seminar-ai",
                TenantId    = hcmut,
                Title       = "Seminar Học thuật: Deep Learning & Computer Vision 2026",
                Description = "Hội thảo học thuật chuyên sâu về học sâu và thị giác máy tính. "
                            + "Diễn giả từ Google DeepMind, VinAI và các trường ĐH quốc tế. "
                            + "Ưu tiên sinh viên năm 3-4 và học viên cao học ngành CNTT, Kỹ thuật Điện tử.",
                Location    = "Hội trường H1 - Bách Khoa TP.HCM",
                StartTime   = now.AddDays(6).Date.AddHours(8),
                EndTime     = now.AddDays(6).Date.AddHours(17),
                Capacity    = 300,
                ImageUrl    = "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-3),
                CreatedAt   = now.AddDays(-8),
                UpdatedAt   = now.AddDays(-3)
            },
            new()
            {
                Id          = "hcmut-evt-career-fair",
                TenantId    = hcmut,
                Title       = "Ngày hội Việc làm BK Career Fair 2026",
                Description = "Hội chợ việc làm thường niên quy mô lớn nhất tại ĐH Bách Khoa TP.HCM. "
                            + "Hơn 150 doanh nghiệp tham gia tuyển dụng, thực tập. "
                            + "Mang hồ sơ CV, có workshop hướng dẫn phỏng vấn và xây dựng CV miễn phí.",
                Location    = "Sảnh A - Khu A, Bách Khoa TP.HCM",
                StartTime   = now.AddDays(15).Date.AddHours(8),
                EndTime     = now.AddDays(15).Date.AddHours(17),
                Capacity    = 2000,
                ImageUrl    = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-5),
                CreatedAt   = now.AddDays(-10),
                UpdatedAt   = now.AddDays(-5)
            },
            new()
            {
                Id          = "hcmut-evt-checkin-demo",
                TenantId    = hcmut,
                Title       = "Demo Check-in: Hội thảo Kỹ thuật Bách Khoa",
                Description = "Sự kiện đang diễn ra ngay bây giờ. Dùng mã BK2026 để test tính năng check-in.",
                Location    = "Phòng Hội thảo H6-101, Bách Khoa TP.HCM",
                StartTime   = now.AddMinutes(-30),
                EndTime     = now.AddHours(3),
                Capacity    = 100,
                ImageUrl    = "https://images.unsplash.com/photo-1552664730-d307ca884978?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-1),
                CheckInCode = "BK2026",
                CheckInCodeExpiredAt = now.AddHours(3),
                CreatedAt   = now.AddDays(-2),
                UpdatedAt   = now.AddDays(-1)
            },

            // ── HCMUT: Đã kết thúc (2 ngày trước) ────────────────────────────
            new()
            {
                Id          = "hcmut-evt-ended-olympiad",
                TenantId    = hcmut,
                Title       = "Olympic Tin học Bách Khoa 2026",
                Description = "Kỳ thi Olympic Tin học thường niên giữa các trường đại học kỹ thuật đã kết thúc 2 ngày trước. "
                            + "Đội HCMUT giành giải Nhất toàn đoàn với thành tích xuất sắc tại vòng chung kết.",
                Location    = "Phòng thi C6 - Khu C, Bách Khoa TP.HCM",
                StartTime   = now.AddDays(-3).Date.AddHours(7.5),  // Bắt đầu 3 ngày trước
                EndTime     = now.AddDays(-2).Date.AddHours(17),   // Kết thúc 2 ngày trước
                Capacity    = 300,
                ImageUrl    = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Approved,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-14),
                CreatedAt   = now.AddDays(-21),
                UpdatedAt   = now.AddDays(-2)
            },

            // ── HCMUT: Đã hủy ──────────────────────────────────────────────────
            new()
            {
                Id          = "hcmut-evt-cancelled-trip",
                TenantId    = hcmut,
                Title       = "Chuyến thăm quan Nhà máy & Thực địa Kỹ thuật BK 2026 [ĐÃ HỦY]",
                Description = "Chuyến thăm quan thực địa tại khu công nghiệp Đồng Nai đã bị hủy do doanh nghiệp tiếp đón tạm dừng "
                            + "hoạt động tham quan vì lý do nội bộ. Khoa Kỹ thuật Điện sẽ lên kế hoạch chuyến thay thế trong học kỳ sau.",
                Location    = "Khu Công nghiệp Nhơn Trạch - Đồng Nai",
                StartTime   = now.AddDays(4).Date.AddHours(7),   // Dự kiến 4 ngày nữa nhưng đã bị hủy
                EndTime     = now.AddDays(4).Date.AddHours(18),
                Capacity    = 60,
                ImageUrl    = "https://images.unsplash.com/photo-1565043666747-69f6646db940?w=800&auto=format&fit=crop&q=60",
                OrganizerId = "admin-hcmut",
                Status      = EventStatus.Cancelled,
                ApprovedById = "admin-hcmut",
                ApprovedAt  = now.AddDays(-10),
                CreatedAt   = now.AddDays(-18),
                UpdatedAt   = now.AddDays(-1)
            },
        });
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

    public Task<bool> UpdateEventAsync(Event ev)
    {
        lock (_lock)
        {
            var idx = _events.FindIndex(e => e.Id == ev.Id);
            if (idx >= 0)
            {
                _events[idx] = ev;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteEventAsync(string eventId, string tenantId) => Task.FromResult(false);

    public Task<bool> ApproveEventAsync(string eventId, string tenantId, string approvedById) => Task.FromResult(false);

    public Task<bool> RejectEventAsync(string eventId, string tenantId, string approvedById, string reason) => Task.FromResult(false);
}
