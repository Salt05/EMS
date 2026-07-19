using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.Services;

namespace EMS.Mvc.Services;

/// <summary>
/// In-memory implementation of IRegistrationService for Development environment.
/// </summary>
public class DevInMemoryRegistrationService : IRegistrationService
{
    private static readonly List<Registration> Registrations = new();
    private readonly IEventService _eventService;

    public DevInMemoryRegistrationService(IEventService eventService)
    {
        _eventService = eventService;
    }

    public Task<Registration?> GetRegistrationByIdAsync(string registrationId, string tenantId)
    {
        var reg = Registrations.FirstOrDefault(r => r.Id == registrationId && r.TenantId == tenantId);
        return Task.FromResult(reg);
    }

    public Task<List<Registration>> GetRegistrationsByStudentAsync(string studentEmail, string tenantId)
    {
        var result = Registrations
            .Where(r => r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase) && r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId, RegistrationStatus? status = null)
    {
        var result = Registrations
            .Where(r => r.EventId == eventId && r.TenantId == tenantId && (!status.HasValue || r.Status == status.Value))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<Registration>> GetRegistrationsByUserAsync(string userId, string tenantId)
    {
        var result = Registrations
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<(Registration? Registration, string? Error)> RegisterAsync(string eventId, string tenantId, string userId, string? note)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelAsync(string registrationId, string tenantId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ApproveAsync(string registrationId, string tenantId, string processedById)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RejectAsync(string registrationId, string tenantId, string processedById, string reason)
    {
        throw new NotImplementedException();
    }

    public Task<(Registration? Registration, string? Error)> GenerateCheckInCodeAsync(string eventId, string tenantId, string userId)
    {
        return Task.FromResult<(Registration?, string?)>((null, "Not implemented in dev mode"));
    }

    public Task<bool> MarkAsPaidAsync(string registrationId, string tenantId, string transactionId, decimal amount, decimal platformFeePercentage)
    {
        return Task.FromResult(true);
    }

    public Task<int> ExpirePendingPaymentsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = Registrations.Where(registration =>
            registration.Status == RegistrationStatus.PendingPayment &&
            registration.PaymentExpiresAt.HasValue &&
            registration.PaymentExpiresAt.Value <= now).ToList();

        foreach (var registration in expired)
        {
            registration.Status = RegistrationStatus.Cancelled;
            registration.CancelledAt = now;
            registration.UpdatedAt = now;
        }

        return Task.FromResult(expired.Count);
    }

    public Task<(Registration? Registration, string? Error)> ValidateCheckInAsync(string code, string tenantId, string requesterUserId, bool requesterIsAdminOrManager)
    {
        throw new NotImplementedException();
    }

    public async Task<Registration?> RegisterForEventAsync(string tenantId, string eventId, string studentEmail, string studentName)
    {
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null)
        {
            throw new NotFoundException("Không tìm thấy sự kiện.");
        }

        if (ev.Status != EventStatus.Approved)
        {
            throw new BusinessRuleException("Sự kiện chưa được phê duyệt nên không thể đăng ký.");
        }

        if (ev.EndTime < DateTime.UtcNow)
        {
            throw new BusinessRuleException("Sự kiện đã kết thúc.");
        }

        var existingReg = Registrations.FirstOrDefault(r => 
            r.EventId == eventId && 
            r.TenantId == tenantId && 
            r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

        if (existingReg != null)
        {
            if (existingReg.Status == RegistrationStatus.Approved || existingReg.Status == RegistrationStatus.Pending)
            {
                throw new BusinessRuleException("Bạn đã đăng ký tham gia sự kiện này rồi.");
            }
        }

        var approvedCount = Registrations.Count(r => 
            r.EventId == eventId && 
            r.TenantId == tenantId && 
            r.Status == RegistrationStatus.Approved);

        if (approvedCount >= ev.Capacity)
        {
            throw new BusinessRuleException("Sự kiện đã hết chỗ.");
        }

        if (existingReg != null)
        {
            existingReg.Status = RegistrationStatus.Approved;
            existingReg.UpdatedAt = DateTime.UtcNow;
            return existingReg;
        }

        var reg = new Registration
        {
            TenantId = tenantId,
            EventId = eventId,
            StudentEmail = studentEmail,
            StudentName = studentName,
            Status = RegistrationStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Registrations.Add(reg);
        return reg;
    }

    public Task<bool> CancelRegistrationAsync(string tenantId, string eventId, string studentEmail)
    {
        var reg = Registrations.FirstOrDefault(r => 
            r.EventId == eventId && 
            r.TenantId == tenantId && 
            r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

        if (reg == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin đăng ký của bạn cho sự kiện này.");
        }

        if (reg.Status == RegistrationStatus.Cancelled)
        {
            return Task.FromResult(true);
        }

        reg.Status = RegistrationStatus.Cancelled;
        reg.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public async Task<(bool Success, string Message)> CheckInAsync(string tenantId, string eventId, string studentEmail, string checkInCode)
    {
        // 1. Lấy thông tin sự kiện
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null)
            return (false, "Không tìm thấy sự kiện.");

        // 2. Kiểm tra sự kiện đang diễn ra
        var now = DateTime.UtcNow;
        if (now < ev.StartTime)
            return (false, "Sự kiện chưa bắt đầu, chưa thể check-in.");
        if (now > ev.EndTime)
            return (false, "Sự kiện đã kết thúc, không thể check-in.");

        // 3. Kiểm tra mã check-in hợp lệ
        if (string.IsNullOrWhiteSpace(ev.CheckInCode))
            return (false, "Sự kiện chưa có mã check-in. Vui lòng liên hệ ban tổ chức.");

        if (!string.Equals(ev.CheckInCode.Trim(), checkInCode.Trim(), StringComparison.OrdinalIgnoreCase))
            return (false, "Mã check-in không đúng. Vui lòng kiểm tra lại.");

        if (ev.CheckInCodeExpiresAt.HasValue && now > ev.CheckInCodeExpiresAt.Value)
            return (false, "Mã check-in đã hết hạn. Vui lòng xin mã mới từ ban tổ chức.");

        // 4. Tìm đăng ký hợp lệ của sinh viên
        var reg = Registrations.FirstOrDefault(r =>
            r.EventId == eventId &&
            r.TenantId == tenantId &&
            r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

        if (reg == null)
            return (false, "Bạn chưa đăng ký tham gia sự kiện này.");

        if (reg.Status != RegistrationStatus.Approved)
            return (false, "Đăng ký của bạn chưa được phê duyệt.");

        // 5. Kiểm tra đã check-in rồi chưa
        if (reg.CheckedIn)
            return (false, $"Bạn đã check-in sự kiện này lúc {reg.CheckedInAt?.ToLocalTime().ToString("HH:mm dd/MM/yyyy")}.");

        // 6. Thực hiện check-in
        reg.CheckedIn = true;
        reg.CheckedInAt = now;
        reg.UpdatedAt = now;

        return (true, $"Check-in thành công! Chào mừng bạn đến với \"{ev.Title}\".");
    }
}
