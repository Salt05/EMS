using EMS.Core.Entities;
using EMS.Core.Entities.Enums;

namespace EMS.Core.Interfaces.Services;

public interface IRegistrationService
{
    Task<Registration?> GetRegistrationByIdAsync(string registrationId, string tenantId);
    Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId, RegistrationStatus? status = null);
    Task<List<Registration>> GetRegistrationsByUserAsync(string userId, string tenantId);

    /// <summary>
    /// Registers a user for an event. On success returns the created registration and a null error.
    /// On failure returns a null registration and a human-readable reason
    /// (event not found / not open / duplicate registration).
    /// </summary>
    Task<(Registration? Registration, string? Error)> RegisterAsync(string eventId, string tenantId, string userId, string? note);

    Task<bool> CancelAsync(string registrationId, string tenantId);
    Task<bool> ApproveAsync(string registrationId, string tenantId, string processedById);
    Task<bool> RejectAsync(string registrationId, string tenantId, string processedById, string reason);
}
