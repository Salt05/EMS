using EMS.Shared.DTOs.Registrations;
using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public static class RegistrationsMock
{
    public static List<RegistrationResponseDto> Registrations = new()
    {
        new RegistrationResponseDto
        {
            Id = "reg1",
            TenantId = "huflit",
            EventId = "evt1",
            UserId = "user1",
            Note = "Mong muốn học hỏi thêm kiến thức mới.",
            Status = 2, // Confirmed
            StatusName = "Confirmed",
            RegisteredAt = DateTime.UtcNow.AddDays(-4),
            ProcessedById = "user3",
            ProcessedAt = DateTime.UtcNow.AddDays(-2),
            CheckedIn = true,
            CheckedInAt = DateTime.UtcNow.AddDays(-2).AddMinutes(15),
            CheckInCode = "CK8293",
            UserFullName = "Nguyễn Văn A (DEMO)",
            UserEmail = "a@huflit.edu.vn",
            UserMSSV = "2023001"
        },
        new RegistrationResponseDto
        {
            Id = "reg2",
            TenantId = "huflit",
            EventId = "evt1",
            UserId = "user4",
            Note = "Đăng ký nhóm 3 người.",
            Status = 1, // Pending
            StatusName = "Pending",
            RegisteredAt = DateTime.UtcNow.AddDays(-3),
            CheckedIn = false,
            UserFullName = "Lê Thị D (DEMO)",
            UserEmail = "d@hcmut.edu.vn",
            UserMSSV = "2023004"
        },
        new RegistrationResponseDto
        {
            Id = "reg3",
            TenantId = "huflit",
            EventId = "evt1",
            UserId = "user5",
            Note = null,
            Status = 6, // Rejected
            StatusName = "Rejected",
            RegisteredAt = DateTime.UtcNow.AddDays(-2),
            ProcessedById = "user3",
            ProcessedAt = DateTime.UtcNow.AddDays(-1),
            RejectionReason = "Không đủ điều kiện tham gia.",
            CheckedIn = false,
            UserFullName = "Hoàng Văn E (DEMO)",
            UserEmail = "e@hcmut.edu.vn",
            UserMSSV = "2023005"
        },
        new RegistrationResponseDto
        {
            Id = "reg4",
            TenantId = "huflit",
            EventId = "evt3",
            UserId = "user1",
            Note = "Đăng ký tham dự seminar.",
            Status = 2, // Confirmed
            StatusName = "Confirmed",
            RegisteredAt = DateTime.UtcNow.AddDays(-1),
            ProcessedById = "user3",
            ProcessedAt = DateTime.UtcNow.AddMinutes(-30),
            CheckedIn = true,
            CheckedInAt = DateTime.UtcNow.AddMinutes(-5),
            CheckInCode = "AI8812",
            UserFullName = "Nguyễn Văn A (DEMO)",
            UserEmail = "a@huflit.edu.vn",
            UserMSSV = "2023001"
        },
        new RegistrationResponseDto
        {
            Id = "reg5",
            TenantId = "huflit",
            EventId = "evt3",
            UserId = "user4",
            Note = "Mong muốn kết nối doanh nghiệp.",
            Status = 2, // Confirmed
            StatusName = "Confirmed",
            RegisteredAt = DateTime.UtcNow.AddDays(-1),
            ProcessedById = "user3",
            ProcessedAt = DateTime.UtcNow.AddMinutes(-20),
            CheckedIn = false,
            CheckInCode = "AI9921",
            UserFullName = "Lê Thị D (DEMO)",
            UserEmail = "d@hcmut.edu.vn",
            UserMSSV = "2023004"
        }
    };
}
