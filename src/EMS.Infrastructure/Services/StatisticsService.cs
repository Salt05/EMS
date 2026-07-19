using System.Globalization;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

/// <summary>
/// Computes dashboard statistics by aggregating data from the event, user,
/// tenant and registration services. All heavy lifting happens in-memory after
/// a small number of Firestore reads, which is adequate for the current scale.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IEventService _eventService;
    private readonly IUserService _userService;
    private readonly ITenantService _tenantService;
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        IEventService eventService,
        IUserService userService,
        ITenantService tenantService,
        IRegistrationService registrationService,
        ILogger<StatisticsService> logger)
    {
        _eventService = eventService;
        _userService = userService;
        _tenantService = tenantService;
        _registrationService = registrationService;
        _logger = logger;
    }

    public async Task<SuperAdminDashboardStatsDto> GetSuperAdminStatsAsync()
    {
        var tenants = await _tenantService.GetTenantsAsync();

        var tenantEventCounts = new List<TenantEventCountDto>();
        var roleCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalUsers = 0;
        int totalEvents = 0;

        foreach (var tenant in tenants)
        {
            var events = await _eventService.GetEventsByTenantAsync(tenant.Id);
            totalEvents += events.Count;
            tenantEventCounts.Add(new TenantEventCountDto
            {
                TenantName = tenant.Name,
                EventCount = events.Count
            });

            var users = await _userService.GetUsersByTenantAsync(tenant.Id);
            totalUsers += users.Count;
            foreach (var user in users)
            {
                var role = user.RoleIds.FirstOrDefault() ?? "unknown";
                roleCounts[role] = roleCounts.GetValueOrDefault(role) + 1;
            }
        }

        var roleRatios = roleCounts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new RoleRatioDto
            {
                RoleName = kv.Key,
                UserCount = kv.Value,
                Ratio = totalUsers > 0 ? Math.Round(kv.Value * 100.0 / totalUsers, 1) : 0
            })
            .ToList();

        // Compute total platform fee across all tenants
        decimal totalPlatformFee = 0;
        foreach (var tenant in tenants)
        {
            var events = await _eventService.GetEventsByTenantAsync(tenant.Id);
            foreach (var ev in events)
            {
                var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenant.Id);
                totalPlatformFee += regs.Where(r => r.IsPaid).Sum(r => r.PlatformFee);
            }
        }

        return new SuperAdminDashboardStatsDto
        {
            TotalTenants = tenants.Count,
            TotalUsers = totalUsers,
            TotalEvents = totalEvents,
            TopTenants = tenantEventCounts
                .OrderByDescending(t => t.EventCount)
                .Take(5)
                .ToList(),
            RoleRatios = roleRatios,
            TotalPlatformFee = totalPlatformFee
        };
    }

    public async Task<TenantAdminDashboardStatsDto> GetTenantAdminStatsAsync(string tenantId)
    {
        var events = await _eventService.GetEventsByTenantAsync(tenantId);
        var users = await _userService.GetUsersByTenantAsync(tenantId);
        var now = DateTime.UtcNow;

        int totalRegistrations = 0;
        int totalCheckedIn = 0;
        foreach (var ev in events)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var active = regs.Where(IsActive).ToList();
            totalRegistrations += active.Count;
            totalCheckedIn += active.Count(r => r.CheckedIn);
        }

        var userMap = users.ToDictionary(u => u.Id, u => u.FullName);

        var topOrganizers = events
            .GroupBy(e => e.OrganizerId)
            .Select(g => new OrganizerEventCountDto
            {
                OrganizerName = userMap.TryGetValue(g.Key, out var name) && !string.IsNullOrEmpty(name)
                    ? name
                    : g.Key,
                EventCount = g.Count()
            })
            .OrderByDescending(o => o.EventCount)
            .Take(5)
            .ToList();

        // Revenue
        decimal totalRevenue = 0;
        decimal totalPlatformFee = 0;
        foreach (var ev in events)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var paidRegs = regs.Where(r => r.IsPaid).ToList();
            totalRevenue += paidRegs.Sum(r => r.OrganizerRevenue);
            totalPlatformFee += paidRegs.Sum(r => r.PlatformFee);
        }

        return new TenantAdminDashboardStatsDto
        {
            TotalUsers = users.Count,
            TotalEvents = events.Count,
            OngoingEvents = events.Count(e => IsOngoing(e, now)),
            CheckInRate = totalRegistrations > 0
                ? Math.Round(totalCheckedIn * 100.0 / totalRegistrations, 1)
                : 0,
            TopOrganizers = topOrganizers,
            MonthlyEvents = BuildMonthlySeries(events, 6),
            TotalRevenue = totalRevenue,
            TotalPlatformFee = totalPlatformFee
        };
    }

    public async Task<OrganizerDashboardStatsDto> GetOrganizerStatsAsync(string tenantId, string organizerId)
    {
        var allEvents = await _eventService.GetEventsByTenantAsync(tenantId);
        var events = allEvents.Where(e => e.OrganizerId == organizerId).ToList();
        var now = DateTime.UtcNow;

        int totalRegistrations = 0;
        int totalCheckedIn = 0;
        var eventRegistrations = new List<EventRegistrationCountDto>();

        foreach (var ev in events)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var active = regs.Where(IsActive).ToList();
            totalRegistrations += active.Count;
            totalCheckedIn += active.Count(r => r.CheckedIn);

            eventRegistrations.Add(new EventRegistrationCountDto
            {
                EventTitle = ev.Title,
                RegistrationCount = active.Count
            });
        }

        // Revenue
        decimal totalRevenue = 0;
        decimal totalPlatformFee = 0;
        foreach (var ev in events)
        {
            var regs = await _registrationService.GetRegistrationsByEventAsync(ev.Id, tenantId);
            var paidRegs = regs.Where(r => r.IsPaid).ToList();
            totalRevenue += paidRegs.Sum(r => r.OrganizerRevenue);
            totalPlatformFee += paidRegs.Sum(r => r.PlatformFee);
        }

        return new OrganizerDashboardStatsDto
        {
            TotalEvents = events.Count,
            OngoingEvents = events.Count(e => IsOngoing(e, now)),
            UpcomingEvents = events.Count(e => e.StartTime > now && e.Status != EventStatus.Cancelled && e.Status != EventStatus.Rejected),
            CheckInRate = totalRegistrations > 0
                ? Math.Round(totalCheckedIn * 100.0 / totalRegistrations, 1)
                : 0,
            EventRegistrations = eventRegistrations
                .OrderByDescending(e => e.RegistrationCount)
                .Take(10)
                .ToList(),
            TotalRevenue = totalRevenue,
            PlatformFee = totalPlatformFee
        };
    }

    // ============ SINGLE EVENT STATS (P8_E2_F1_T3) ============

    public async Task<EventStatsDto> GetEventStatsAsync(string tenantId, string eventId)
    {
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null) throw new InvalidOperationException($"Event {eventId} not found");

        var regs = await _registrationService.GetRegistrationsByEventAsync(eventId, tenantId);

        return new EventStatsDto
        {
            EventId = ev.Id,
            EventTitle = ev.Title,
            StatusName = ev.Status.ToString(),
            Capacity = ev.Capacity,
            TotalRegistrations = regs.Count(IsActive),
            Confirmed = regs.Count(r => r.Status == RegistrationStatus.Confirmed),
            Pending = regs.Count(r => r.Status == RegistrationStatus.Pending),
            Cancelled = regs.Count(r => r.Status == RegistrationStatus.Cancelled),
            CheckedIn = regs.Count(r => r.CheckedIn),
            CheckInRate = regs.Count(IsActive) > 0
                ? Math.Round(regs.Count(r => r.CheckedIn) * 100.0 / regs.Count(IsActive), 1)
                : 0,
            TotalRevenue = regs.Where(r => r.IsPaid).Sum(r => r.OrganizerRevenue + r.PlatformFee),
            PlatformFee = regs.Where(r => r.IsPaid).Sum(r => r.PlatformFee)
        };
    }

    // ============ HELPERS ============

    // A registration counts towards attendance figures unless it was cancelled or rejected.
    private static bool IsActive(Registration r) =>
        r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Rejected;

    private static bool IsOngoing(Event e, DateTime now) =>
        e.Status != EventStatus.Cancelled &&
        e.Status != EventStatus.Rejected &&
        e.StartTime <= now && e.EndTime >= now;

    // Builds a contiguous month series ending with the current month, so the
    // chart always shows a fixed window even for months with no events.
    private static List<MonthlyEventCountDto> BuildMonthlySeries(List<Event> events, int months)
    {
        var now = DateTime.UtcNow;
        var counts = events
            .GroupBy(e => new DateTime(e.StartTime.Year, e.StartTime.Month, 1))
            .ToDictionary(g => g.Key, g => g.Count());

        var series = new List<MonthlyEventCountDto>();
        for (int i = months - 1; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            series.Add(new MonthlyEventCountDto
            {
                Month = month.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                EventCount = counts.GetValueOrDefault(month)
            });
        }
        return series;
    }
}
