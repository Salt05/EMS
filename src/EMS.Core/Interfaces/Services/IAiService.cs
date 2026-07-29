using EMS.Shared.DTOs;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Dịch vụ AI: sinh nội dung sự kiện và trả lời câu hỏi chatbot.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sinh nội dung sự kiện (tiêu đề, mô tả, địa điểm, sức chứa, lịch trình) từ ý tưởng ngắn.
    /// </summary>
    Task<AiGeneratedEventDto> GenerateEventContentAsync(AiGenerateEventRequestDto request);

    /// <summary>
    /// Trả lời câu hỏi chatbot cho sinh viên dựa trên danh sách sự kiện hiện có.
    /// </summary>
    Task<AiChatResponseDto> ChatWithAssistantAsync(AiChatRequestDto request, List<string> eventSummaries);

    /// <summary>
    /// Phân tích Intent của User (List hay Detail) và viết lại câu hỏi hoàn chỉnh.
    /// </summary>
    Task<AiChatIntentDto> AnalyzeChatIntentAsync(AiChatRequestDto request);
}
