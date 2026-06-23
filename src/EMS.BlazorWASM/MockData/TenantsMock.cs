using EMS.Shared.DTOs;
using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public static class TenantsMock
{
    public static List<TenantDTO> Tenants = new()
    {
        new TenantDTO
        {
            Id = "huflit",
            Name = "ĐH HUFLIT (DEMO)",
            Subdomain = "huflit",
            Email = "contact@huflit.edu.vn",
            PhoneNumber = "02838632052",
            Address = "828 Sư Vạn Hạnh, P.13, Q.10, TP.HCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-12)
        },
        new TenantDTO
        {
            Id = "bachkhoa",
            Name = "ĐH Bách Khoa (DEMO)",
            Subdomain = "hcmut",
            Email = "contact@hcmut.edu.vn",
            PhoneNumber = "02838647256",
            Address = "268 Lý Thường Kiệt, P.14, Q.10, TP.HCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-10)
        },
        new TenantDTO
        {
            Id = "khtn",
            Name = "ĐH Khoa Học Tự Nhiên (DEMO)",
            Subdomain = "hcmus",
            Email = "contact@hcmus.edu.vn",
            PhoneNumber = "02838353135",
            Address = "227 Nguyễn Văn Cừ, P.4, Q.5, TP.HCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-8)
        },
        new TenantDTO
        {
            Id = "ftu",
            Name = "ĐH Ngoại Thương CS2 (DEMO)",
            Subdomain = "ftu",
            Email = "contact@ftu.edu.vn",
            PhoneNumber = "02835127254",
            Address = "15 Đường D5, P.25, Q.Bình Thạnh, TP.HCM",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        },
        new TenantDTO
        {
            Id = "tdt",
            Name = "ĐH Tôn Đức Thắng (DEMO)",
            Subdomain = "tdtu",
            Email = "contact@tdtu.edu.vn",
            PhoneNumber = "02837755035",
            Address = "19 Nguyễn Hữu Thọ, P.Tân Phong, Q.7, TP.HCM",
            IsActive = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-4)
        }
    };
}
