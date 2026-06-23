using EMS.Shared.DTOs.Registrations;

namespace EMS.BlazorWASM.Services;

public interface IRegistrationServiceClient
{
    Task<List<RegistrationResponseDto>> GetEventRegistrationsAsync(string eventId, int? status = null);
    Task<bool> ApproveRegistrationAsync(string registrationId);
    Task<bool> RejectRegistrationAsync(string registrationId, string reason);
}
