namespace EMS.Shared.DTOs;

/// <summary>
/// Request DTO: Người dùng gửi ý tưởng ngắn để AI sinh nội dung sự kiện.
/// </summary>
public class AiGenerateEventRequestDto
{
    /// <summary>Ý tưởng / chủ đề sự kiện (ví dụ: "Workshop lập trình C# cho sinh viên năm 2")</summary>
    public string TopicPrompt { get; set; } = string.Empty;

    /// <summary>Danh mục gợi ý (tùy chọn): Seminar, Workshop, Hội thảo, v.v.</summary>
    public string? Category { get; set; }

    /// <summary>Thời gian bắt đầu sự kiện (nếu đã chọn trước trên form)</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>Thời gian kết thúc sự kiện (nếu đã chọn trước trên form)</summary>
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// Response DTO: Nội dung sự kiện do AI sinh ra, sẵn sàng auto-fill vào form.
/// </summary>
public class AiGeneratedEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SuggestedLocation { get; set; } = string.Empty;
    public int SuggestedCapacity { get; set; }
    public List<AiAgendaItemDto> AgendaItems { get; set; } = new();
}

/// <summary>
/// Một mốc lịch trình do AI gợi ý.
/// </summary>
public class AiAgendaItemDto
{
    public string Title { get; set; } = string.Empty;
    public int StartMinuteOffset { get; set; }
    public int DurationMinutes { get; set; }
}

/// <summary>
/// Đại diện một tin nhắn trong lịch sử chat để AI có ngữ cảnh cuộc hội thoại.
/// </summary>
public class AiChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" hoặc "model"
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO: Sinh viên gửi câu hỏi cho AI Chatbot tư vấn sự kiện.
/// </summary>
public class AiChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? UserEmail { get; set; }
    public List<AiChatMessageDto> History { get; set; } = new();
}

/// <summary>
/// Response DTO: AI trả lời cho Chatbot.
/// </summary>
public class AiChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public bool IsFromAi { get; set; } = true;
}

/// <summary>
/// DTO chứa phân tích Intent từ Gemini (List vs Detail)
/// </summary>
public class AiChatIntentDto
{
    public string Intent { get; set; } = "List";
    public string SearchKeyword { get; set; } = string.Empty;
    public string StandaloneQuery { get; set; } = string.Empty;
}
