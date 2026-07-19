using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Service interface for event registrations.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Gets a registration by its unique identifier.
    /// </summary>
    Task<Registration?> GetRegistrationByIdAsync(string registrationId, string tenantId);

    /// <summary>
    /// Gets registrations for a specific event with optional status filtering.
    /// </summary>
    Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId, RegistrationStatus? status = null);

    /// <summary>
    /// Gets registrations for a specific user ID.
    /// </summary>
    Task<List<Registration>> GetRegistrationsByUserAsync(string userId, string tenantId);

    /// <summary>
    /// Registers a user for an event. On success returns the created registration and a null error.
    /// On failure returns a null registration and a human-readable reason.
    /// </summary>
    Task<(Registration? Registration, string? Error)> RegisterAsync(string eventId, string tenantId, string userId, string? note);

    /// <summary>
    /// Cancels a registration by ID.
    /// </summary>
    Task<bool> CancelAsync(string registrationId, string tenantId);

    /// <summary>
    /// Approves a registration.
    /// </summary>
    Task<bool> ApproveAsync(string registrationId, string tenantId, string processedById);

    /// <summary>
    /// Rejects a registration.
    /// </summary>
    Task<bool> RejectAsync(string registrationId, string tenantId, string processedById, string reason);

    /// <summary>
    /// Marks a registration as paid.
    /// </summary>
    Task<bool> MarkAsPaidAsync(string registrationId, string tenantId, string transactionId, decimal amount, decimal platformFeePercentage);

    /// <summary>Expires unpaid registrations whose payment reservation has elapsed.</summary>
    Task<int> ExpirePendingPaymentsAsync();

    /// <summary>
    /// Generates (or refreshes) a check-in code for the given user's confirmed registration to an event.
    /// </summary>
    Task<(Registration? Registration, string? Error)> GenerateCheckInCodeAsync(string eventId, string tenantId, string userId);

    /// <summary>
    /// Validates a check-in code and marks the matching registration as checked in.
    /// </summary>
    Task<(Registration? Registration, string? Error)> ValidateCheckInAsync(
        string code, string tenantId, string requesterUserId, bool requesterIsAdminOrManager);

    /// <summary>Sentinel error returned by ValidateCheckInAsync when the requester lacks permission.</summary>
    public const string ForbiddenError = "FORBIDDEN";

    // =========================================================================
    // MVC Student Portal compatibility methods
    // =========================================================================

    /// <summary>
    /// Gets all registrations for a specific student email.
    /// </summary>
    Task<List<Registration>> GetRegistrationsByStudentAsync(string studentEmail, string tenantId);

    /// <summary>
    /// Registers a student for an event via email/name.
    /// </summary>
    Task<Registration?> RegisterForEventAsync(string tenantId, string eventId, string studentEmail, string studentName);

    /// <summary>
    /// Cancels a student's registration for an event.
    /// </summary>
    Task<bool> CancelRegistrationAsync(string tenantId, string eventId, string studentEmail);

    /// <summary>
    /// Performs check-in for a registered student using a check-in code.
    /// </summary>
    Task<(bool Success, string Message)> CheckInAsync(string tenantId, string eventId, string studentEmail, string checkInCode);
}
