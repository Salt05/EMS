using EMS.Shared.DTOs;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public static class DashboardStatsMock
{
    public static SuperAdminDashboardStatsDto SuperAdminStats => new()
    {
        TotalTenants = 6,
        TotalUsers = 3850,
        TotalEvents = 124,
        TopTenants = new List<TenantEventCountDto>
        {
            new() { TenantName = "ĐH HUFLIT", EventCount = 45 },
            new() { TenantName = "ĐH Bách Khoa", EventCount = 32 },
            new() { TenantName = "ĐH KHTN", EventCount = 20 },
            new() { TenantName = "ĐH Ngoại Thương", EventCount = 15 },
            new() { TenantName = "ĐH Tôn Đức Thắng", EventCount = 12 }
        },
        RoleRatios = new List<RoleRatioDto>
        {
            new() { RoleName = "Student", Ratio = 90.1, UserCount = 3470 },
            new() { RoleName = "Organizer", Ratio = 7.3, UserCount = 280 },
            new() { RoleName = "Tenant Admin", Ratio = 2.3, UserCount = 90 },
            new() { RoleName = "Super Admin", Ratio = 0.3, UserCount = 10 }
        }
    };

    public static TenantAdminDashboardStatsDto TenantAdminStats => new()
    {
        TotalUsers = 1500,
        TotalEvents = 45,
        OngoingEvents = 3,
        CheckInRate = 78.4,
        TopOrganizers = new List<OrganizerEventCountDto>
        {
            new() { OrganizerName = "CLB Tin học", EventCount = 12 },
            new() { OrganizerName = "CLB Tiếng Anh", EventCount = 8 },
            new() { OrganizerName = "Đoàn Khoa CNTT", EventCount = 7 },
            new() { OrganizerName = "Đoàn Trường", EventCount = 5 },
            new() { OrganizerName = "CLB Mỹ Thuật", EventCount = 4 }
        },
        MonthlyEvents = new List<MonthlyEventCountDto>
        {
            new() { Month = "Thg 1", EventCount = 4 },
            new() { Month = "Thg 2", EventCount = 3 },
            new() { Month = "Thg 3", EventCount = 8 },
            new() { Month = "Thg 4", EventCount = 5 },
            new() { Month = "Thg 5", EventCount = 10 },
            new() { Month = "Thg 6", EventCount = 12 }
        }
    };

    public static OrganizerDashboardStatsDto OrganizerStats => new()
    {
        TotalEvents = 12,
        OngoingEvents = 1,
        UpcomingEvents = 2,
        CheckInRate = 82.5,
        EventRegistrations = new List<EventRegistrationCountDto>
        {
            new() { EventTitle = "Hackathon 2026", RegistrationCount = 150 },
            new() { EventTitle = "Workshop Git & GitHub", RegistrationCount = 85 },
            new() { EventTitle = "Seminar AI & Deep Learning", RegistrationCount = 120 },
            new() { EventTitle = "Coding Contest", RegistrationCount = 60 },
            new() { EventTitle = "Báo cáo đề tài NCKH", RegistrationCount = 40 }
        }
    };
}
