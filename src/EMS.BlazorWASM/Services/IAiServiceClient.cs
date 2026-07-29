using EMS.Shared.DTOs;

namespace EMS.BlazorWASM.Services;

public interface IAiServiceClient
{
    Task<AiGeneratedEventDto?> GenerateEventContentAsync(AiGenerateEventRequestDto request);
    Task<AiChatResponseDto?> ChatAsync(AiChatRequestDto request);
}
