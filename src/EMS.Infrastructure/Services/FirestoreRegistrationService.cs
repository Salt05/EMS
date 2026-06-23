using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

public class FirestoreRegistrationService : IRegistrationService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IEventService _eventService;
    private readonly ILogger<FirestoreRegistrationService> _logger;
    private const string CollectionName = "registrations";

    public FirestoreRegistrationService(
        FirestoreDb firestoreDb,
        IEventService eventService,
        ILogger<FirestoreRegistrationService> logger)
    {
        _firestoreDb = firestoreDb;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<Registration?> GetRegistrationByIdAsync(string registrationId, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).Document(registrationId).GetSnapshotAsync();

            if (!snapshot.Exists) return null;

            var reg = MapToRegistration(snapshot);
            if (reg.TenantId != tenantId) return null;

            return reg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registration by id {registrationId}");
            return null;
        }
    }

    public async Task<List<Registration>> GetRegistrationsByStudentAsync(string studentEmail, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("studentEmail", studentEmail)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToRegistration)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registrations for student {studentEmail}");
            return new List<Registration>();
        }
    }

    public async Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("eventId", eventId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToRegistration)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registrations for event {eventId}");
            return new List<Registration>();
        }
    }

    public async Task<Registration?> RegisterForEventAsync(string tenantId, string eventId, string studentEmail, string studentName)
    {
        _logger.LogInformation($"RegisterForEventAsync called for event {eventId}, student {studentEmail}");

        // 1. Get Event
        var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
        if (ev == null)
        {
            throw new NotFoundException("Không tìm thấy sự kiện.");
        }

        // 2. Validate Event Status
        if (ev.Status != EventStatus.Approved)
        {
            throw new BusinessRuleException("Sự kiện chưa được phê duyệt nên không thể đăng ký.");
        }

        // 3. Validate Event Time
        if (ev.EndTime < DateTime.UtcNow)
        {
            throw new BusinessRuleException("Sự kiện đã kết thúc.");
        }

        // 4. Check existing registration
        var studentRegs = await GetRegistrationsByStudentAsync(studentEmail, tenantId);
        var existingReg = studentRegs.FirstOrDefault(r => r.EventId == eventId);

        if (existingReg != null)
        {
            if (existingReg.Status == RegistrationStatus.Approved || existingReg.Status == RegistrationStatus.Pending)
            {
                throw new BusinessRuleException("Bạn đã đăng ký tham gia sự kiện này rồi.");
            }
        }

        // 5. Check Capacity
        var eventRegs = await GetRegistrationsByEventAsync(eventId, tenantId);
        var approvedCount = eventRegs.Count(r => r.Status == RegistrationStatus.Approved);
        if (approvedCount >= ev.Capacity)
        {
            throw new BusinessRuleException("Sự kiện đã hết chỗ.");
        }

        // 6. Create or update registration
        var reg = existingReg ?? new Registration
        {
            TenantId = tenantId,
            EventId = eventId,
            StudentEmail = studentEmail,
            StudentName = studentName
        };

        reg.Status = RegistrationStatus.Approved; // Automatically approve registrations for simple flow
        reg.UpdatedAt = DateTime.UtcNow;
        if (existingReg == null)
        {
            reg.CreatedAt = DateTime.UtcNow;
        }

        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
            await docRef.SetAsync(reg.ToFirestoreDocument());
            _logger.LogInformation($"Registration success: {reg.Id}");
            return reg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error writing registration to Firestore for event {eventId}, student {studentEmail}");
            throw new BusinessRuleException("Đã xảy ra lỗi hệ thống khi đăng ký. Vui lòng thử lại sau.", ex);
        }
    }

    public async Task<bool> CancelRegistrationAsync(string tenantId, string eventId, string studentEmail)
    {
        _logger.LogInformation($"CancelRegistrationAsync called for event {eventId}, student {studentEmail}");

        var studentRegs = await GetRegistrationsByStudentAsync(studentEmail, tenantId);
        var reg = studentRegs.FirstOrDefault(r => r.EventId == eventId);

        if (reg == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin đăng ký của bạn cho sự kiện này.");
        }

        if (reg.Status == RegistrationStatus.Cancelled)
        {
            return true;
        }

        reg.Status = RegistrationStatus.Cancelled;
        reg.UpdatedAt = DateTime.UtcNow;

        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
            await docRef.SetAsync(reg.ToFirestoreDocument(), SetOptions.MergeAll);
            _logger.LogInformation($"Registration cancelled: {reg.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling registration {reg.Id}");
            throw new BusinessRuleException("Đã xảy ra lỗi hệ thống khi hủy đăng ký. Vui lòng thử lại sau.", ex);
        }
    }

    private static Registration MapToRegistration(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();

        return new Registration
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            TenantId = dict.TryGetValue("tenantId", out var tid) ? tid?.ToString() ?? "" : "",
            EventId = dict.TryGetValue("eventId", out var eid) ? eid?.ToString() ?? "" : "",
            StudentEmail = dict.TryGetValue("studentEmail", out var email) ? email?.ToString() ?? "" : "",
            StudentName = dict.TryGetValue("studentName", out var name) ? name?.ToString() ?? "" : "",
            Status = dict.TryGetValue("status", out var st) && st is long stLong ? (RegistrationStatus)(int)stLong : RegistrationStatus.Pending,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }
}
