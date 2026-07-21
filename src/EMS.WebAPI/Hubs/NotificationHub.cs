using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EMS.WebAPI.Hubs;

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Connection started: {Context.ConnectionId}");
        var roles = Context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .ToList();
            
        Console.WriteLine($"[SignalR] User IsAuthenticated: {Context.User?.Identity?.IsAuthenticated}");
            
        if (roles != null && roles.Any())
        {
            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role.ToLower());
            }

            var tenantId = Context.User?.Claims.FirstOrDefault(c => c.Type == "tenantId" || c.Type == "tenant")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
                foreach (var role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"{role.ToLower()}_{tenantId}");
                }
            }
        }
        else
        {
            // Fallback cho MVC Client (không có JWT) -> auto gán vào group student để nhận số chỗ
            await Groups.AddToGroupAsync(Context.ConnectionId, "student");
        }
        
        await base.OnConnectedAsync();
    }
}
