using System;
using System.Collections.Generic;

namespace EMS.BlazorWASM.MockData;

public class MockEmailTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public static class EmailTemplatesMock
{
    public static List<MockEmailTemplateDto> Templates = new()
    {
        new MockEmailTemplateDto
        {
            Id = "temp1",
            TenantId = "huflit",
            Name = "Xác nhận đăng ký tham gia sự kiện (DEMO)",
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
        new MockEmailTemplateDto
        {
            Id = "temp2",
            TenantId = "huflit",
            Name = "Nhắc nhở tham gia sự kiện (DEMO)",
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
        new MockEmailTemplateDto
        {
            Id = "temp3",
            TenantId = "huflit",
            Name = "Thông báo hủy sự kiện (DEMO)",
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
