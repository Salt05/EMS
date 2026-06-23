using Hangfire.Dashboard;

namespace EMS.WebAPI.Filters;

/// <summary>
/// Restricts Hangfire Dashboard access to authenticated users with Admin or Manager roles.
/// In Development mode this filter is not registered (see Program.cs).
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
            return false;

        return user.IsInRole("admin") || user.IsInRole("Admin")
            || user.IsInRole("manager") || user.IsInRole("Manager");
    }
}
