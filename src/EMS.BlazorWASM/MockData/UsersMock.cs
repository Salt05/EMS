using EMS.Shared.DTOs.Admin;
using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public static class UsersMock
{
    public static List<AdminUserItemDto> Users = new()
    {
        new AdminUserItemDto
        {
            Id = "user1",
            MSSV = "2023001",
            FullName = "Nguyễn Văn A (DEMO)",
            Email = "a@huflit.edu.vn",
            PhoneNumber = "0901234567",
            Department = "Công nghệ thông tin",
            RoleIds = new List<string> { "employee" },
            TenantId = "huflit",
            TenantName = "ĐH HUFLIT",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-11)
        },
        new AdminUserItemDto
        {
            Id = "user2",
            MSSV = "2023002",
            FullName = "Trần Thị B (DEMO)",
            Email = "b@huflit.edu.vn",
            PhoneNumber = "0902345678",
            Department = "Ngoại ngữ",
            RoleIds = new List<string> { "manager" },
            TenantId = "huflit",
            TenantName = "ĐH HUFLIT",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        },
        new AdminUserItemDto
        {
            Id = "user3",
            MSSV = "2023003",
            FullName = "Phạm Văn C (DEMO)",
            Email = "c@huflit.edu.vn",
            PhoneNumber = "0903456789",
            Department = "Quản trị kinh doanh",
            RoleIds = new List<string> { "admin" },
            TenantId = "huflit",
            TenantName = "ĐH HUFLIT",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-9)
        },
        new AdminUserItemDto
        {
            Id = "user4",
            MSSV = "2023004",
            FullName = "Lê Thị D (DEMO)",
            Email = "d@hcmut.edu.vn",
            PhoneNumber = "0904567890",
            Department = "Khoa Học Máy Tính",
            RoleIds = new List<string> { "employee" },
            TenantId = "bachkhoa",
            TenantName = "ĐH Bách Khoa",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        },
        new AdminUserItemDto
        {
            Id = "user5",
            MSSV = "2023005",
            FullName = "Hoàng Văn E (DEMO)",
            Email = "e@hcmut.edu.vn",
            PhoneNumber = "0905678901",
            Department = "Điện - Điện tử",
            RoleIds = new List<string> { "manager" },
            TenantId = "bachkhoa",
            TenantName = "ĐH Bách Khoa",
            Status = 2,
            StatusName = "Inactive",
            CreatedAt = DateTime.UtcNow.AddMonths(-7)
        },
        new AdminUserItemDto
        {
            Id = "user6",
            MSSV = null,
            FullName = "Super Admin (DEMO)",
            Email = "superadmin@ems.local",
            PhoneNumber = "0900000000",
            Department = "Ban Giám Hiệu",
            RoleIds = new List<string> { "superadmin" },
            TenantId = "system",
            TenantName = "Hệ thống",
            Status = 1,
            StatusName = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-12)
        }
    };
}
