using EMS.Shared.DTOs.CheckIns;
using EMS.Shared.DTOs.Registrations;

namespace EMS.BlazorWASM.Services;

public interface ICheckInServiceClient
{
    Task<CheckInResponseDto?> ValidateCheckInCodeAsync(string code);
    Task<List<RegistrationResponseDto>> GetAttendeesAsync(string eventId);
}
