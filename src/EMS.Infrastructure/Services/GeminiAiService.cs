using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

/// <summary>
/// Triển khai IAiService sử dụng Google Gemini API (gemini-2.0-flash).
/// Tích hợp Smart Fallback: nếu không có API Key hoặc lỗi mạng, tự sinh nội dung thông minh.
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiAiService> _logger;
    private readonly string? _apiKey;
    private const string GroqBaseUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GeminiAiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiAiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"];
    }

    public async Task<AiGeneratedEventDto> GenerateEventContentAsync(AiGenerateEventRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Gemini API Key chưa được cấu hình. Sử dụng Smart Fallback Generator.");
            return GenerateSmartFallback(request);
        }

        try
        {
            var prompt = BuildEventGenerationPrompt(request);
            var aiResponse = await CallGeminiApiAsync(prompt);
            var parsed = ParseEventResponse(aiResponse);
            if (parsed != null) return parsed;

            _logger.LogWarning("Không thể parse response từ Gemini. Fallback.");
            return GenerateSmartFallback(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini API. Sử dụng Smart Fallback.");
            return GenerateSmartFallback(request);
        }
    }

    public async Task<AiChatResponseDto> ChatWithAssistantAsync(AiChatRequestDto request, List<string> eventSummaries)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return GenerateChatFallback(request, eventSummaries);
        }

        try
        {
            var systemPrompt = BuildChatSystemPrompt(eventSummaries);
            var aiResponse = await CallGeminiChatApiAsync(request, systemPrompt);
            return new AiChatResponseDto { Reply = aiResponse, IsFromAi = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini Chat API. Sử dụng Fallback.");
            return GenerateChatFallback(request, eventSummaries);
        }
    }

    public async Task<AiChatIntentDto> AnalyzeChatIntentAsync(AiChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            // C# Fallback logic
            var msg = request.Message.ToLower();
            if (msg.Contains("lúc mấy giờ") || msg.Contains("địa điểm") || msg.Contains("ở đâu") || msg.Contains("khi nào") || msg.Contains("thưởng") || msg.Contains("giá vé"))
                return new AiChatIntentDto { Intent = "Detail", SearchKeyword = string.Empty, StandaloneQuery = request.Message };
            return new AiChatIntentDto { Intent = "List", SearchKeyword = string.Empty, StandaloneQuery = request.Message };
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dựa vào lịch sử hội thoại và câu hỏi mới nhất của người dùng, hãy phân tích ý định (Intent) của họ.");
            sb.AppendLine("Nếu họ đang hỏi chung chung, tìm kiếm danh sách sự kiện, hãy trả về Intent: 'List'.");
            sb.AppendLine("Nếu họ đang hỏi chi tiết về một sự kiện cụ thể (hoặc một câu hỏi nối tiếp về thời gian, địa điểm, phần thưởng của sự kiện vừa được nhắc đến), hãy trả về Intent: 'Detail', đồng thời trích xuất tên sự kiện hoặc từ khóa chính vào 'SearchKeyword'.");
            sb.AppendLine("Ngoài ra, hãy viết lại câu hỏi mới nhất thành một 'StandaloneQuery' độc lập, đầy đủ ngữ cảnh.");
            sb.AppendLine("Bạn PHẢI trả về KẾT QUẢ DƯỚI DẠNG JSON. Ví dụ: {\"intent\": \"Detail\", \"searchKeyword\": \"test quà 1\", \"standaloneQuery\": \"sự kiện test quà 1 ở đâu\"}");
            sb.AppendLine();
            sb.AppendLine("Lịch sử hội thoại:");
            foreach (var h in request.History.TakeLast(4))
            {
                sb.AppendLine($"{h.Role}: {h.Content}");
            }
            sb.AppendLine($"user: {request.Message}");

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) EMS-AI/1.0");
            
            var payload = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = sb.ToString() }
                },
                temperature = 0.1,
                max_tokens = 500,
                response_format = new { type = "json_object" }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GroqBaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Strip markdown code block if Gemini still returns it despite application/json
                var cleanJson = text.Trim();
                if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanJson = cleanJson.Substring(7).Trim();
                }
                if (cleanJson.StartsWith("```"))
                {
                    cleanJson = cleanJson.Substring(3).Trim();
                }
                if (cleanJson.EndsWith("```"))
                {
                    cleanJson = cleanJson.Substring(0, cleanJson.Length - 3).Trim();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiChatIntentDto>(cleanJson, options);
                if (result != null) return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Gemini AnalyzeChatIntentAsync. Dùng fallback.");
        }

        return new AiChatIntentDto { Intent = "List", SearchKeyword = string.Empty, StandaloneQuery = request.Message };
    }

    // ============ GEMINI API CALLS ============

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) EMS-AI/1.0");

        var payload = new
        {
            model = "llama-3.1-8b-instant",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 2048
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(GroqBaseUrl, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? string.Empty;
    }

    private async Task<string> CallGeminiChatApiAsync(AiChatRequestDto request, string systemPrompt)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) EMS-AI/1.0");

        var messagesList = new List<object>();

        // System prompt
        messagesList.Add(new { role = "system", content = systemPrompt });

        // Map history to Groq payload format
        if (request.History != null && request.History.Any())
        {
            foreach (var msg in request.History)
            {
                if (string.IsNullOrWhiteSpace(msg.Content)) continue;
                var role = msg.Role.ToLower() == "user" ? "user" : "assistant";
                messagesList.Add(new { role = role, content = msg.Content });
            }
        }

        // Add the current user message
        messagesList.Add(new { role = "user", content = request.Message });

        var payload = new
        {
            model = "llama-3.1-8b-instant",
            messages = messagesList.ToArray(),
            temperature = 0.7,
            max_tokens = 2048
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(GroqBaseUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API Error Response: {Err}", errContent);
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return text ?? string.Empty;
    }

    // ============ PROMPT BUILDERS ============

    private string BuildEventGenerationPrompt(AiGenerateEventRequestDto request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý AI chuyên tạo nội dung sự kiện cho hệ thống quản lý sự kiện sinh viên đại học.");
        sb.AppendLine("Dựa trên ý tưởng bên dưới, hãy tạo nội dung sự kiện hoàn chỉnh bằng tiếng Việt.");
        sb.AppendLine();
        sb.AppendLine("YÊU CẦU BẮT BUỘC:");
        sb.AppendLine("- Trả về ĐÚNG định dạng JSON (không có markdown, không có ```json)");
        sb.AppendLine("- Mô tả sự kiện phải hấp dẫn, chuyên nghiệp, từ 150-300 từ");
        sb.AppendLine("- Lịch trình (agendaItems) phải có 4-6 mốc thời gian hợp lý");
        sb.AppendLine("- Mỗi mốc lịch trình có: title, startMinuteOffset (phút tính từ lúc bắt đầu sự kiện), durationMinutes");
        sb.AppendLine("- Địa điểm phải cụ thể (tên phòng/hội trường)");
        sb.AppendLine();
        sb.AppendLine("ĐỊNH DẠNG JSON:");
        sb.AppendLine("{");
        sb.AppendLine("  \"title\": \"Tên sự kiện đầy đủ\",");
        sb.AppendLine("  \"description\": \"Mô tả chi tiết...\",");
        sb.AppendLine("  \"suggestedLocation\": \"Hội trường A1, Tòa nhà B2\",");
        sb.AppendLine("  \"suggestedCapacity\": 200,");
        sb.AppendLine("  \"agendaItems\": [");
        sb.AppendLine("    { \"title\": \"Đón tiếp & Check-in\", \"startMinuteOffset\": 0, \"durationMinutes\": 30 },");
        sb.AppendLine("    { \"title\": \"Khai mạc\", \"startMinuteOffset\": 30, \"durationMinutes\": 15 }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"Ý TƯỞNG SỰ KIỆN: {request.TopicPrompt}");

        if (!string.IsNullOrEmpty(request.Category))
            sb.AppendLine($"DANH MỤC: {request.Category}");

        return sb.ToString();
    }

    private string BuildChatSystemPrompt(List<string> eventSummaries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Dữ liệu sự kiện tìm được từ DB]:");
        
        if (eventSummaries.Any())
        {
            foreach (var ev in eventSummaries)
            {
                sb.AppendLine(ev);
            }
        }
        else
        {
            sb.AppendLine("- (Hiện tại chưa có sự kiện nào trong hệ thống)");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== PHONG CÁCH & NÚT HÀNH ĐỘNG SỰ KIỆN ===");
        sb.AppendLine("1. PHONG CÁCH: Trả lời tự nhiên, thân thiện, sinh động. Tuyệt đối KHÔNG in danh sách các dòng debug thô dạng '- Tên: ... | ID: ... | ImageUrl: ...' cho người dùng. Hãy tóm tắt lại bằng văn phong tự nhiên.");
        sb.AppendLine("2. NÚT 'HIỂN THỊ SỰ KIỆN': Mỗi khi bạn giới thiệu hoặc trả lời thông tin của bất kỳ sự kiện nào, ở cuối dòng thông tin đó, bạn BẮT BỤỢC PHẢI đính kèm mã nút bấm theo cú pháp:");
        sb.AppendLine("   [BTN: id=ID_SỰ_KIỆN | title=TÊN_SỰ_KIỆN]");
        sb.AppendLine("   Ví dụ: Sự kiện **sự kiện sắp tới 999** sẽ diễn ra từ 30/07 đến 31/07 tại Trái đất. [BTN: id=12d339c3-cad8-42a2-be68-05b014fb81dc | title=sự kiện sắp tới 999]");
        sb.AppendLine("3. THẺ SỰ KIỆN CHI TIẾT (CARD): CHỈ KHI người dùng gửi câu nhắn yêu cầu xem/hiển thị thẻ sự kiện (ví dụ chứa 'Hiển thị thẻ sự kiện...'), bạn MỚI ĐƯỢC đính kèm mã thẻ ở cuối câu:");
        sb.AppendLine("   [CARD: id=ID_SỰ_KIỆN | title=TÊN_SỰ_KIỆN | image=IMAGE_URL | time=THỜI_GIAN | location=ĐỊA_ĐIỂM | capacity=SỨC_CHỨA | price=GIÁ_VÉ]");
        sb.AppendLine("4. TRA CỨU SINH VIÊN: Khi được hỏi về danh sách sinh viên hoặc email của người đăng ký sự kiện, bạn PHẢI tra cứu mục 'Danh sách sinh viên đăng ký' trong dữ liệu để liệt kê đầy đủ Tên và Email sinh viên cho người dùng.");

        return sb.ToString();
    }

    // ============ RESPONSE PARSER ============

    private AiGeneratedEventDto? ParseEventResponse(string aiResponse)
    {
        try
        {
            // Loại bỏ markdown code block nếu Gemini trả về
            var json = aiResponse.Trim();
            if (json.StartsWith("```"))
            {
                var firstNewLine = json.IndexOf('\n');
                if (firstNewLine >= 0) json = json[(firstNewLine + 1)..];
                if (json.EndsWith("```")) json = json[..^3];
                json = json.Trim();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<AiGeneratedEventDto>(json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Parse AI JSON thất bại. Raw: {Response}", aiResponse[..Math.Min(200, aiResponse.Length)]);
            return null;
        }
    }

    // ============ SMART FALLBACK GENERATORS ============

    private AiGeneratedEventDto GenerateSmartFallback(AiGenerateEventRequestDto request)
    {
        var topic = request.TopicPrompt.Trim();
        var category = request.Category ?? DetectCategory(topic);

        var (title, description, location, capacity, agendaItems) = category.ToLower() switch
        {
            "workshop" => GenerateWorkshopContent(topic),
            "seminar" or "hội thảo" => GenerateSeminarContent(topic),
            "cuộc thi" or "hackathon" => GenerateCompetitionContent(topic),
            "networking" or "giao lưu" => GenerateNetworkingContent(topic),
            _ => GenerateGenericContent(topic, category)
        };

        return new AiGeneratedEventDto
        {
            Title = title,
            Description = description,
            SuggestedLocation = location,
            SuggestedCapacity = capacity,
            AgendaItems = agendaItems
        };
    }

    private string DetectCategory(string topic)
    {
        var lower = topic.ToLower();
        if (lower.Contains("workshop") || lower.Contains("thực hành") || lower.Contains("hands-on"))
            return "workshop";
        if (lower.Contains("seminar") || lower.Contains("hội thảo") || lower.Contains("chuyên đề"))
            return "seminar";
        if (lower.Contains("hackathon") || lower.Contains("cuộc thi") || lower.Contains("competition"))
            return "cuộc thi";
        if (lower.Contains("giao lưu") || lower.Contains("networking") || lower.Contains("meetup"))
            return "networking";
        return "sự kiện";
    }

    private (string title, string desc, string location, int capacity, List<AiAgendaItemDto> agenda) GenerateWorkshopContent(string topic)
    {
        var title = topic.Length > 10 ? topic : $"Workshop: {topic}";
        var desc = $"🔧 **{title}**\n\n"
            + $"Chào mừng các bạn sinh viên đến với {title} — một buổi workshop thực hành chuyên sâu được tổ chức dành riêng cho sinh viên!\n\n"
            + "📌 **Nội dung chương trình:**\n"
            + "• Tổng quan lý thuyết nền tảng và các khái niệm cốt lõi\n"
            + "• Demo trực tiếp từ diễn giả có kinh nghiệm thực tế\n"
            + "• Phiên thực hành hands-on với hướng dẫn từng bước\n"
            + "• Q&A trực tiếp và giải đáp thắc mắc cá nhân\n\n"
            + "🎁 **Lợi ích khi tham gia:**\n"
            + "• Nhận chứng chỉ hoàn thành workshop\n"
            + "• Cộng điểm rèn luyện theo quy định\n"
            + "• Kết nối với cộng đồng và chuyên gia trong ngành\n"
            + "• Tài liệu và source code được chia sẻ sau buổi học\n\n"
            + "⚠️ **Lưu ý:** Vui lòng mang theo laptop cá nhân đã cài đặt sẵn các công cụ cần thiết. Hướng dẫn cài đặt sẽ được gửi qua email sau khi đăng ký thành công.";

        return (title, desc, "Phòng Lab A3-201, Tòa nhà A3", 60, new List<AiAgendaItemDto>
        {
            new() { Title = "Đón tiếp & Check-in", StartMinuteOffset = 0, DurationMinutes = 20 },
            new() { Title = "Giới thiệu tổng quan & Lý thuyết nền tảng", StartMinuteOffset = 20, DurationMinutes = 40 },
            new() { Title = "Demo trực tiếp từ diễn giả", StartMinuteOffset = 60, DurationMinutes = 30 },
            new() { Title = "Thực hành Hands-on (Phần 1)", StartMinuteOffset = 90, DurationMinutes = 45 },
            new() { Title = "Giải lao & Networking", StartMinuteOffset = 135, DurationMinutes = 15 },
            new() { Title = "Thực hành Hands-on (Phần 2) & Q&A", StartMinuteOffset = 150, DurationMinutes = 45 },
            new() { Title = "Tổng kết & Trao chứng chỉ", StartMinuteOffset = 195, DurationMinutes = 15 }
        });
    }

    private (string title, string desc, string location, int capacity, List<AiAgendaItemDto> agenda) GenerateSeminarContent(string topic)
    {
        var title = topic.Length > 10 ? topic : $"Hội thảo: {topic}";
        var desc = $"🎓 **{title}**\n\n"
            + $"Buổi hội thảo chuyên đề \"{title}\" quy tụ các chuyên gia hàng đầu, mang đến kiến thức thực tiễn và cái nhìn sâu sắc cho sinh viên.\n\n"
            + "📌 **Điểm nhấn chương trình:**\n"
            + "• Keynote từ diễn giả khách mời đặc biệt\n"
            + "• Panel Discussion với các chuyên gia đa lĩnh vực\n"
            + "• Phiên hỏi đáp tương tác trực tiếp\n"
            + "• Networking coffee break cùng diễn giả\n\n"
            + "🎁 **Quyền lợi người tham dự:**\n"
            + "• Cộng điểm rèn luyện theo quy định của nhà trường\n"
            + "• Nhận tài liệu chuyên sâu và slide trình bày\n"
            + "• Cơ hội kết nối và đặt câu hỏi trực tiếp với diễn giả\n\n"
            + "📣 Số lượng chỗ ngồi có hạn, đăng ký sớm để đảm bảo vị trí!";

        return (title, desc, "Hội trường lớn B1-101, Tòa nhà B1", 200, new List<AiAgendaItemDto>
        {
            new() { Title = "Đón tiếp & Check-in đại biểu", StartMinuteOffset = 0, DurationMinutes = 30 },
            new() { Title = "Khai mạc & Giới thiệu diễn giả", StartMinuteOffset = 30, DurationMinutes = 10 },
            new() { Title = "Keynote: Bài trình bày chính", StartMinuteOffset = 40, DurationMinutes = 45 },
            new() { Title = "Panel Discussion", StartMinuteOffset = 85, DurationMinutes = 35 },
            new() { Title = "Giải lao & Coffee Networking", StartMinuteOffset = 120, DurationMinutes = 15 },
            new() { Title = "Phiên Q&A tương tác", StartMinuteOffset = 135, DurationMinutes = 20 },
            new() { Title = "Tổng kết & Bế mạc", StartMinuteOffset = 155, DurationMinutes = 10 }
        });
    }

    private (string title, string desc, string location, int capacity, List<AiAgendaItemDto> agenda) GenerateCompetitionContent(string topic)
    {
        var title = topic.Length > 10 ? topic : $"Cuộc thi: {topic}";
        var desc = $"🏆 **{title}**\n\n"
            + $"Sân chơi cạnh tranh lành mạnh dành cho sinh viên đam mê và muốn thử thách bản thân!\n\n"
            + "📌 **Thể lệ cuộc thi:**\n"
            + "• Thi theo cá nhân hoặc nhóm (tối đa 3-4 thành viên)\n"
            + "• Đề bài được công bố tại buổi thi\n"
            + "• Thời gian làm bài và trình bày theo quy định\n"
            + "• Ban giám khảo chấm điểm công khai, minh bạch\n\n"
            + "🎁 **Giải thưởng hấp dẫn:**\n"
            + "• Giải Nhất: Phần thưởng giá trị + Chứng nhận\n"
            + "• Giải Nhì & Ba: Phần thưởng + Chứng nhận\n"
            + "• Tất cả thí sinh tham gia đều được cộng điểm rèn luyện\n\n"
            + "⚡ Đăng ký ngay để không bỏ lỡ cơ hội thể hiện tài năng!";

        return (title, desc, "Phòng Hội thảo C2-301, Tòa nhà C2", 100, new List<AiAgendaItemDto>
        {
            new() { Title = "Đón tiếp & Check-in thí sinh", StartMinuteOffset = 0, DurationMinutes = 30 },
            new() { Title = "Khai mạc & Phổ biến thể lệ", StartMinuteOffset = 30, DurationMinutes = 15 },
            new() { Title = "Vòng thi chính", StartMinuteOffset = 45, DurationMinutes = 90 },
            new() { Title = "Giải lao", StartMinuteOffset = 135, DurationMinutes = 15 },
            new() { Title = "Trình bày & Chấm điểm", StartMinuteOffset = 150, DurationMinutes = 45 },
            new() { Title = "Công bố kết quả & Trao giải", StartMinuteOffset = 195, DurationMinutes = 15 }
        });
    }

    private (string title, string desc, string location, int capacity, List<AiAgendaItemDto> agenda) GenerateNetworkingContent(string topic)
    {
        var title = topic.Length > 10 ? topic : $"Giao lưu: {topic}";
        var desc = $"🤝 **{title}**\n\n"
            + $"Buổi giao lưu kết nối giúp sinh viên mở rộng mối quan hệ, chia sẻ kinh nghiệm và học hỏi từ những người đi trước.\n\n"
            + "📌 **Hoạt động chính:**\n"
            + "• Chia sẻ kinh nghiệm từ anh chị cựu sinh viên / chuyên gia\n"
            + "• Hoạt động team-building và ice-breaking vui nhộn\n"
            + "• Speed networking — kết nối nhanh với nhiều người\n"
            + "• Thảo luận nhóm theo chủ đề\n\n"
            + "🎁 **Quyền lợi:**\n"
            + "• Mở rộng network chất lượng\n"
            + "• Cộng điểm rèn luyện\n"
            + "• Nhận quà lưu niệm\n\n"
            + "🌟 Không gian thân thiện, cởi mở — hãy đến và kết nối!";

        return (title, desc, "Sảnh đa năng D1, Tòa nhà D1", 80, new List<AiAgendaItemDto>
        {
            new() { Title = "Đón tiếp & Ice-breaking", StartMinuteOffset = 0, DurationMinutes = 20 },
            new() { Title = "Chia sẻ kinh nghiệm từ khách mời", StartMinuteOffset = 20, DurationMinutes = 30 },
            new() { Title = "Hoạt động Team-building", StartMinuteOffset = 50, DurationMinutes = 30 },
            new() { Title = "Speed Networking", StartMinuteOffset = 80, DurationMinutes = 25 },
            new() { Title = "Thảo luận nhóm & Chia sẻ tự do", StartMinuteOffset = 105, DurationMinutes = 20 },
            new() { Title = "Chụp ảnh kỷ niệm & Bế mạc", StartMinuteOffset = 125, DurationMinutes = 10 }
        });
    }

    private (string title, string desc, string location, int capacity, List<AiAgendaItemDto> agenda) GenerateGenericContent(string topic, string category)
    {
        var title = topic.Length > 10 ? topic : $"{category}: {topic}";
        var desc = $"✨ **{title}**\n\n"
            + $"Sự kiện \"{title}\" được tổ chức nhằm mang đến trải nghiệm bổ ích và ý nghĩa cho sinh viên.\n\n"
            + "📌 **Nội dung chương trình:**\n"
            + "• Phần trình bày chính từ diễn giả/ban tổ chức\n"
            + "• Hoạt động tương tác và thảo luận\n"
            + "• Phiên hỏi đáp mở\n\n"
            + "🎁 **Quyền lợi khi tham gia:**\n"
            + "• Cộng điểm rèn luyện theo quy định\n"
            + "• Nhận tài liệu và quà tặng\n"
            + "• Kết nối cộng đồng sinh viên\n\n"
            + "📣 Đăng ký ngay để giữ chỗ!";

        return (title, desc, "Hội trường A1-101, Tòa nhà A1", 150, new List<AiAgendaItemDto>
        {
            new() { Title = "Đón tiếp & Check-in", StartMinuteOffset = 0, DurationMinutes = 20 },
            new() { Title = "Khai mạc chương trình", StartMinuteOffset = 20, DurationMinutes = 10 },
            new() { Title = "Phần trình bày chính", StartMinuteOffset = 30, DurationMinutes = 45 },
            new() { Title = "Hoạt động tương tác & Thảo luận", StartMinuteOffset = 75, DurationMinutes = 30 },
            new() { Title = "Giải lao", StartMinuteOffset = 105, DurationMinutes = 15 },
            new() { Title = "Phiên Q&A mở & Tổng kết", StartMinuteOffset = 120, DurationMinutes = 20 }
        });
    }

    private AiChatResponseDto GenerateChatFallback(AiChatRequestDto request, List<string> eventSummaries)
    {
        // Loại bỏ hoàn toàn việc tự build chuỗi thủ công theo yêu cầu (Tránh Hardcoded template).
        // Nếu API Gemini bị lỗi hoặc chưa cấu hình, trả về thông báo lỗi chung.
        return new AiChatResponseDto 
        { 
            Reply = "⚠️ Hệ thống AI hiện đang bảo trì hoặc quá tải. Vui lòng liên hệ ban quản trị hoặc thử lại sau nhé! 😊", 
            IsFromAi = false 
        };
    }
}
