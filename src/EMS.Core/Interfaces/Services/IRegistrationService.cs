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

    /// <summary>
    /// Generates (or refreshes) a check-in code for the given user's confirmed registration to an event.
    /// Returns the updated registration, or an error reason (not registered / not confirmed / event not found).
    /// </summary>
    Task<(Registration? Registration, string? Error)> GenerateCheckInCodeAsync(string eventId, string tenantId, string userId);

    /// <summary>
    /// Validates a check-in code and marks the matching registration as checked in.
    /// Authorization is enforced here so the lookup and the mark happen atomically: only the
    /// event's organizer or an admin/manager may check attendees in. Returns the checked-in
    /// registration, or an error reason (invalid / expired / not confirmed / already checked in).
    /// When the requester is not allowed, the error is <see cref="ForbiddenError"/>.
    /// </summary>
    Task<(Registration? Registration, string? Error)> ValidateCheckInAsync(
        string code, string tenantId, string requesterUserId, bool requesterIsAdminOrManager);

    /// <summary>Sentinel error returned by <see cref="ValidateCheckInAsync"/> when the requester lacks permission.</summary>
    public const string ForbiddenError = "FORBIDDEN";
}
