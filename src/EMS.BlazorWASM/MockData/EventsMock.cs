using EMS.Shared.DTOs.Events;
using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public static class EventsMock
{
    public static List<EventResponseDto> Events = new()
    {
        new EventResponseDto
        {
            Id = "evt1",
            TenantId = "huflit",
            Title = "Hackathon HUFLIT 2026 (DEMO)",
            Description = "Cuộc thi lập trình hackathon kéo dài 24 tiếng dành cho sinh viên.",
            Location = "Hội trường A - HUFLIT",
            VenueId = "venue_hall_a",
            StartTime = DateTime.UtcNow.AddDays(15),
            EndTime = DateTime.UtcNow.AddDays(16),
            Capacity = 100,
            ImageUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?auto=format&fit=crop&w=600&q=80",
            OrganizerId = "user2", // Organizer
            Status = 2, // Approved
            StatusName = "Approved",
            ApprovedById = "user3",
            ApprovedAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        },
        new EventResponseDto
        {
            Id = "evt2",
            TenantId = "huflit",
            Title = "Workshop Git & GitHub (DEMO)",
            Description = "Hướng dẫn quản lý mã nguồn cơ bản với Git và GitHub.",
            Location = "Phòng máy 402 - HUFLIT",
            VenueId = null,
            StartTime = DateTime.UtcNow.AddDays(-2), // Ended
            EndTime = DateTime.UtcNow.AddDays(-2).AddHours(3),
            Capacity = 40,
            ImageUrl = "https://images.unsplash.com/photo-1618401471353-b98aedd07871?auto=format&fit=crop&w=600&q=80",
            OrganizerId = "user2",
            Status = 4, // Ended (actually Status is StatusName = Ended, int value 4 in EventStatus enum)
            StatusName = "Ended",
            ApprovedById = "user3",
            ApprovedAt = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-12),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        },
        new EventResponseDto
        {
            Id = "evt3",
            TenantId = "huflit",
            Title = "Seminar AI & Deep Learning (DEMO)",
            Description = "Cập nhật xu hướng phát triển trí tuệ nhân tạo mới nhất.",
            Location = "Hội trường B - HUFLIT",
            VenueId = "venue_hall_b",
            StartTime = DateTime.UtcNow.AddMinutes(10), // Ongoing
            EndTime = DateTime.UtcNow.AddHours(2),
            Capacity = 150,
            ImageUrl = "https://images.unsplash.com/photo-1677442136019-21780efad99a?auto=format&fit=crop&w=600&q=80",
            OrganizerId = "user2",
            Status = 3, // Ongoing
            StatusName = "Ongoing",
            ApprovedById = "user3",
            ApprovedAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        },
        new EventResponseDto
        {
            Id = "evt4",
            TenantId = "bachkhoa",
            Title = "Bách Khoa Innovation 2026 (DEMO)",
            Description = "Cuộc thi ý tưởng sáng tạo khởi nghiệp Bách Khoa.",
            Location = "Khuôn viên nhà A5 - ĐH Bách Khoa",
            VenueId = null,
            StartTime = DateTime.UtcNow.AddDays(30),
            EndTime = DateTime.UtcNow.AddDays(31),
            Capacity = 200,
            ImageUrl = "https://images.unsplash.com/photo-1515187029135-18ee286d815b?auto=format&fit=crop&w=600&q=80",
            OrganizerId = "user5",
            Status = 1, // Pending
            StatusName = "Pending",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        },
        new EventResponseDto
        {
            Id = "evt5",
            TenantId = "huflit",
            Title = "Giải bóng đá nam HUFLIT 2026 (DEMO)",
            Description = "Giải bóng đá thường niên dành cho sinh viên HUFLIT.",
            Location = "Sân vận động Quân Khu 7",
            VenueId = null,
            StartTime = DateTime.UtcNow.AddDays(40),
            EndTime = DateTime.UtcNow.AddDays(47),
            Capacity = 500,
            ImageUrl = null,
            OrganizerId = "user2",
            Status = 5, // Cancelled
            StatusName = "Cancelled",
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        }
    };
}
