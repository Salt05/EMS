using EMS.Core.Entities;
using EMS.Shared.DTOs.Admin;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.WebAPI.Controllers.Admin;

[ApiController]
[Route("api/admin/email-templates")]
[Authorize(Roles = "admin,superadmin")]
public class EmailTemplatesController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<EmailTemplatesController> _logger;
    private const string CollectionName = "emailTemplates";

    public EmailTemplatesController(FirestoreDb firestoreDb, ILogger<EmailTemplatesController> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTemplates([FromQuery] string? tenantId = null)
    {
        try
        {
            var effectiveTenantId = ResolveTenantScope(tenantId);
            if (string.IsNullOrEmpty(effectiveTenantId)) return BadRequest("Invalid tenant");

            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", effectiveTenantId)
                .GetSnapshotAsync();

            var templates = snapshot.Documents
                .Select(MapToDto)
                .OrderBy(t => t.Name)
                .ToList();

            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email templates");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("defaults")]
    public IActionResult GetDefaultTemplates()
    {
        return Ok(GetSeedTemplates());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTemplateById(string id)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!snapshot.Exists) return NotFound("Email template not found");

            var template = MapToDto(snapshot);
            var effectiveTenantId = ResolveTenantScope(null);
            if (!string.IsNullOrEmpty(effectiveTenantId) && template.TenantId != effectiveTenantId && !IsSuperAdmin())
                return Forbid();

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(string id, [FromBody] EmailTemplateDto dto)
    {
        try
        {
            var effectiveTenantId = ResolveTenantScope(dto.TenantId);
            if (string.IsNullOrEmpty(effectiveTenantId)) return BadRequest("Invalid tenant");

            var docRef = _firestoreDb.Collection(CollectionName).Document(id);
            var snapshot = await docRef.GetSnapshotAsync();
            if (!snapshot.Exists) return NotFound("Email template not found");

            var current = MapToEntity(snapshot);
            if (!IsSuperAdmin() && current.TenantId != effectiveTenantId)
                return Forbid();

            current.Name = dto.Name;
            current.Description = dto.Description;
            current.Subject = dto.Subject;
            current.BodyHtml = dto.BodyHtml;
            current.IsActive = dto.IsActive;
            current.UpdatedAt = DateTime.UtcNow;

            await docRef.SetAsync(current.ToFirestoreDocument(), SetOptions.MergeAll);
            return Ok(MapToDto(current));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email template {TemplateId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private string ResolveTenantScope(string? requestedTenantId)
    {
        if (IsSuperAdmin() && !string.IsNullOrWhiteSpace(requestedTenantId))
        {
            return requestedTenantId;
        }

        return User.FindFirst("tenantId")?.Value ?? string.Empty;
    }

    private bool IsSuperAdmin() => User.Claims.Any(c =>
        (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) &&
        c.Value.Equals("superadmin", StringComparison.OrdinalIgnoreCase));

    private static EmailTemplateDto MapToDto(DocumentSnapshot snapshot) => MapToDto(MapToEntity(snapshot));

    private static EmailTemplate MapToEntity(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();
        return new EmailTemplate
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            TenantId = dict.TryGetValue("tenantId", out var tenantId) ? tenantId?.ToString() ?? string.Empty : string.Empty,
            Name = dict.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
            Description = dict.TryGetValue("description", out var description) ? description?.ToString() ?? string.Empty : string.Empty,
            Subject = dict.TryGetValue("subject", out var subject) ? subject?.ToString() ?? string.Empty : string.Empty,
            BodyHtml = dict.TryGetValue("bodyHtml", out var bodyHtml) ? bodyHtml?.ToString() ?? string.Empty : string.Empty,
            IsActive = dict.TryGetValue("isActive", out var isActive) && isActive is bool active ? active : true,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }

    private static EmailTemplateDto MapToDto(EmailTemplate template) => new()
    {
        Id = template.Id,
        TenantId = template.TenantId,
        Name = template.Name,
        Description = template.Description,
        Subject = template.Subject,
        BodyHtml = template.BodyHtml,
        IsActive = template.IsActive,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt
    };

    private static List<EmailTemplateDto> GetSeedTemplates() => new()
    {
        new EmailTemplateDto
        {
            Id = "temp1",
            TenantId = "huflit",
            Name = "Xác nhận đăng ký tham gia sự kiện",
            Description = "Gửi tự động cho sinh viên khi họ đăng ký thành công.",
            Subject = "Đăng ký thành công sự kiện: {EventTitle}",
            BodyHtml = @"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #E3E8EE; border-radius: 8px;'>
    <h2 style='color: #1A1F36; margin-bottom: 20px;'>Xác nhận đăng ký thành công!</h2>
    <p>Chào bạn <strong>{StudentName}</strong>,</p>
    <p>Hệ thống EMS xin thông báo bạn đã đăng ký tham gia sự kiện <strong>{EventTitle}</strong> thành công.</p>
    <div style='background-color: #F7F9FC; padding: 15px; border-radius: 6px; margin: 20px 0;'>
        <p style='margin: 5px 0;'><strong>Thời gian:</strong> {StartTime}</p>
        <p style='margin: 5px 0;'><strong>Địa điểm:</strong> {Location}</p>
    </div>
    <p>Mã check-in của bạn sẽ được hiển thị trên Cổng thông tin Sinh viên. Vui lòng trình mã này khi điểm danh.</p>
    <p>Trân trọng,<br/>Ban tổ chức {TenantName}</p>
</div>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-5),
            UpdatedAt = DateTime.UtcNow.AddMonths(-5)
        },
        new EmailTemplateDto
        {
            Id = "temp2",
            TenantId = "huflit",
            Name = "Nhắc nhở tham gia sự kiện",
            Description = "Gửi nhắc nhở tự động cho sinh viên trước sự kiện 24 tiếng.",
            Subject = "Nhắc nhở: Sự kiện {EventTitle} sắp diễn ra",
            BodyHtml = @"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #E3E8EE; border-radius: 8px;'>
    <h2 style='color: #1A1F36; margin-bottom: 20px;'>Nhắc nhở sự kiện sắp diễn ra!</h2>
    <p>Chào bạn <strong>{StudentName}</strong>,</p>
    <p>Đây là email nhắc nhở rằng sự kiện <strong>{EventTitle}</strong> bạn đăng ký sẽ bắt đầu vào ngày mai.</p>
    <div style='background-color: #FBF3DB; padding: 15px; border-radius: 6px; margin: 20px 0; color: #956400;'>
        <p style='margin: 5px 0;'><strong>Thời gian:</strong> {StartTime}</p>
        <p style='margin: 5px 0;'><strong>Địa điểm:</strong> {Location}</p>
    </div>
    <p>Vui lòng đến đúng giờ và chuẩn bị mã check-in để ban tổ chức quét mã nhanh chóng.</p>
    <p>Hẹn gặp lại bạn,<br/>Ban tổ chức {TenantName}</p>
</div>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-5),
            UpdatedAt = DateTime.UtcNow.AddMonths(-2)
        },
        new EmailTemplateDto
        {
            Id = "temp3",
            TenantId = "huflit",
            Name = "Thông báo hủy sự kiện",
            Description = "Gửi thông báo cho toàn bộ sinh viên đã đăng ký khi sự kiện bị hủy.",
            Subject = "Thông báo: Hủy sự kiện {EventTitle}",
            BodyHtml = @"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #E3E8EE; border-radius: 8px;'>
    <h2 style='color: #9F2F2D; margin-bottom: 20px;'>Thông báo hủy sự kiện</h2>
    <p>Chào bạn <strong>{StudentName}</strong>,</p>
    <p>Chúng tôi rất tiếc phải thông báo rằng sự kiện <strong>{EventTitle}</strong> dự kiến diễn ra vào {StartTime} đã bị hủy bỏ do lý do bất khả kháng.</p>
    <p><strong>Lý do từ ban tổ chức:</strong> {CancelReason}</p>
    <p>Chúng tôi thành thật xin lỗi vì sự bất tiện này.</p>
    <p>Trân trọng,<br/>Ban tổ chức {TenantName}</p>
</div>",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            UpdatedAt = DateTime.UtcNow.AddMonths(-3)
        }
    };
}