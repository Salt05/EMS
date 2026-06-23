using System.Collections.Generic;

namespace EMS.Shared.DTOs;

public class SuperAdminDashboardStatsDto
{
    public int TotalTenants { get; set; }
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public List<TenantEventCountDto> TopTenants { get; set; } = new();
    public List<RoleRatioDto> RoleRatios { get; set; } = new();
}

public class TenantEventCountDto
{
    public string TenantName { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class RoleRatioDto
{
    public string RoleName { get; set; } = string.Empty;
    public double Ratio { get; set; }
    public int UserCount { get; set; }
}

public class TenantAdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public int OngoingEvents { get; set; }
    public double CheckInRate { get; set; }
    public List<OrganizerEventCountDto> TopOrganizers { get; set; } = new();
    public List<MonthlyEventCountDto> MonthlyEvents { get; set; } = new();
}

public class OrganizerEventCountDto
{
    public string OrganizerName { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class MonthlyEventCountDto
{
    public string Month { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class OrganizerDashboardStatsDto
{
    public int TotalEvents { get; set; }
    public int OngoingEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public double CheckInRate { get; set; }
    public List<EventRegistrationCountDto> EventRegistrations { get; set; } = new();
}

public class EventRegistrationCountDto
{
    public string EventTitle { get; set; } = string.Empty;
    public int RegistrationCount { get; set; }
}
