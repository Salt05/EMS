using EMS.Core.Entities;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Service interface for event registrations.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Gets a registration by its unique identifier.
    /// </summary>
    /// <param name="registrationId">The registration identifier.</param>
    /// <param name="tenantId">The tenant identifier for isolation.</param>
    /// <returns>The registration if found; otherwise, null.</returns>
    Task<Registration?> GetRegistrationByIdAsync(string registrationId, string tenantId);

    /// <summary>
    /// Gets all registrations for a specific student.
    /// </summary>
    /// <param name="studentEmail">The student's email address.</param>
    /// <param name="tenantId">The tenant identifier for isolation.</param>
    /// <returns>A list of registrations.</returns>
    Task<List<Registration>> GetRegistrationsByStudentAsync(string studentEmail, string tenantId);

    /// <summary>
    /// Gets all registrations for a specific event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="tenantId">The tenant identifier for isolation.</param>
    /// <returns>A list of registrations.</returns>
    Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId);

    /// <summary>
    /// Registers a student for an event.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="studentEmail">The student's email address.</param>
    /// <param name="studentName">The student's full name.</param>
    /// <returns>The created or updated registration, or null if registration failed.</returns>
    Task<Registration?> RegisterForEventAsync(string tenantId, string eventId, string studentEmail, string studentName);

    /// <summary>
    /// Cancels a student's registration for an event.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="studentEmail">The student's email address.</param>
    /// <returns>True if cancellation was successful; otherwise, false.</returns>
    Task<bool> CancelRegistrationAsync(string tenantId, string eventId, string studentEmail);
}
