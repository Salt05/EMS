using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services;

public class FirestoreRegistrationService : IRegistrationService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IEventService _eventService;
    private readonly ILogger<FirestoreRegistrationService> _logger;
    private const string CollectionName = "registrations";

    // Statuses that count against an event's capacity (i.e. an active seat is taken/held).
    private static readonly RegistrationStatus[] ActiveStatuses =
    {
        RegistrationStatus.Pending,
        RegistrationStatus.Confirmed,
        RegistrationStatus.PendingPayment
    };

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
            // Tenant isolation: never leak registrations across tenants.
            if (reg.TenantId != tenantId) return null;

            return reg;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registration by id {registrationId}");
            return null;
        }
    }

    public async Task<List<Registration>> GetRegistrationsByEventAsync(string eventId, string tenantId, RegistrationStatus? status = null)
    {
        try
        {
            Query query = _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("eventId", eventId);

            if (status.HasValue)
            {
                query = query.WhereEqualTo("status", (int)status.Value);
            }

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToRegistration)
                .OrderBy(r => r.RegisteredAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registrations for event {eventId}");
            return new List<Registration>();
        }
    }

    public async Task<List<Registration>> GetRegistrationsByUserAsync(string userId, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("userId", userId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToRegistration)
                .OrderByDescending(r => r.RegisteredAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting registrations for user {userId}");
            return new List<Registration>();
        }
    }

    public async Task<(Registration? Registration, string? Error)> RegisterAsync(string eventId, string tenantId, string userId, string? note)
    {
        try
        {
            var result = await _firestoreDb.RunTransactionAsync<(Registration? Registration, string? Error)>(async transaction =>
            {
                // 1. Get Event inside the transaction
                var eventRef = _firestoreDb.Collection("events").Document(eventId);
                var eventSnapshot = await transaction.GetSnapshotAsync(eventRef);
                if (!eventSnapshot.Exists)
                    return (null, "Event not found");

                var evDict = eventSnapshot.ToDictionary();
                var evTenantId = evDict.TryGetValue("tenantId", out var tid) ? tid?.ToString() : "";
                if (evTenantId != tenantId)
                    return (null, "Event not found");

                var evStatusVal = evDict.TryGetValue("status", out var st) && st is long stLong ? (int)stLong : 0;
                if (evStatusVal != (int)EventStatus.Approved)
                    return (null, "Event is not open for registration");

                var evCapacity = evDict.TryGetValue("capacity", out var cap) && cap is long capLong ? (int)capLong : 0;
                var evPrice = evDict.TryGetValue("price", out var price) && price != null ? Convert.ToDecimal(price) : 0m;
                var evEndTime = evDict.TryGetValue("endTime", out var et) && et is Timestamp etTs ? etTs.ToDateTime() : DateTime.MinValue;
                if (evEndTime != DateTime.MinValue && evEndTime.ToUniversalTime() < DateTime.UtcNow)
                    return (null, "Event has already ended");

                // 2. Query registrations inside the transaction
                var regsQuery = _firestoreDb.Collection(CollectionName)
                    .WhereEqualTo("tenantId", tenantId)
                    .WhereEqualTo("eventId", eventId);
                var regsSnapshot = await transaction.GetSnapshotAsync(regsQuery);

                var existing = regsSnapshot.Documents
                    .Select(MapToRegistration)
                    .ToList();

                // Block duplicate registration
                if (existing.Any(r => r.UserId == userId &&
                                      r.Status is RegistrationStatus.Pending
                                               or RegistrationStatus.Confirmed
                                               or RegistrationStatus.PendingPayment
                                               or RegistrationStatus.Waitlisted))
                {
                    return (null, "You are already registered for this event");
                }

                // Check capacity
                var activeCount = existing.Count(r => ActiveStatuses.Contains(r.Status));
                var isFull = evCapacity > 0 && activeCount >= evCapacity;

                var reg = new Registration
                {
                    TenantId = tenantId,
                    EventId = eventId,
                    UserId = userId,
                    Note = note,
                    Status = isFull
                        ? RegistrationStatus.Waitlisted
                        : evPrice > 0 ? RegistrationStatus.PendingPayment : RegistrationStatus.Confirmed,
                    PaymentExpiresAt = !isFull && evPrice > 0 ? DateTime.UtcNow.AddMinutes(5) : null
                };

                var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
                transaction.Set(docRef, reg.ToFirestoreDocument());

                return (reg, null);
            });

            if (result.Registration != null)
            {
                _logger.LogInformation($"Registration created: {result.Registration.Id} (event {eventId}, user {userId}, status {result.Registration.Status})");
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering user {userId} for event {eventId}");
            return (null, "Failed to register");
        }
    }

    public async Task<bool> CancelAsync(string registrationId, string tenantId)
    {
        try
        {
            return await _firestoreDb.RunTransactionAsync<bool>(async transaction =>
            {
                var regRef = _firestoreDb.Collection(CollectionName).Document(registrationId);
                var snapshot = await transaction.GetSnapshotAsync(regRef);
                if (!snapshot.Exists) return false;

                var reg = MapToRegistration(snapshot);
                if (reg.TenantId != tenantId) return false;

                if (reg.Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected)
                    return false;

                var freedSeat = ActiveStatuses.Contains(reg.Status);

                Registration? promotedWaitlist = null;
                var promotedStatus = RegistrationStatus.Confirmed;
                if (freedSeat)
                {
                    promotedWaitlist = await GetWaitlistPromoteTargetInTransactionAsync(transaction, reg.EventId, tenantId);
                    var eventSnapshot = await transaction.GetSnapshotAsync(_firestoreDb.Collection("events").Document(reg.EventId));
                    var eventData = eventSnapshot.Exists ? eventSnapshot.ToDictionary() : null;
                    var eventPrice = eventData != null && eventData.TryGetValue("price", out var price) && price != null
                        ? Convert.ToDecimal(price)
                        : 0m;
                    promotedStatus = eventPrice > 0 ? RegistrationStatus.PendingPayment : RegistrationStatus.Confirmed;
                }

                reg.Status = RegistrationStatus.Cancelled;
                reg.CancelledAt = DateTime.UtcNow;
                reg.UpdatedAt = DateTime.UtcNow;

                transaction.Set(regRef, reg.ToFirestoreDocument(), SetOptions.MergeAll);

                if (promotedWaitlist != null)
                {
                    promotedWaitlist.Status = promotedStatus;
                    promotedWaitlist.PaymentExpiresAt = promotedStatus == RegistrationStatus.PendingPayment
                        ? DateTime.UtcNow.AddMinutes(5)
                        : null;
                    promotedWaitlist.UpdatedAt = DateTime.UtcNow;
                    var promoteRef = _firestoreDb.Collection(CollectionName).Document(promotedWaitlist.Id);
                    transaction.Set(promoteRef, promotedWaitlist.ToFirestoreDocument(), SetOptions.MergeAll);
                    _logger.LogInformation($"Promoted registration {promotedWaitlist.Id} from waitlist for event {reg.EventId} inside transaction");
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling registration {registrationId}");
            return false;
        }
    }

    public async Task<bool> ApproveAsync(string registrationId, string tenantId, string processedById)
    {
        try
        {
            return await _firestoreDb.RunTransactionAsync<bool>(async transaction =>
            {
                var regRef = _firestoreDb.Collection(CollectionName).Document(registrationId);
                var regSnapshot = await transaction.GetSnapshotAsync(regRef);
                if (!regSnapshot.Exists) return false;

                var reg = MapToRegistration(regSnapshot);
                if (reg.TenantId != tenantId) return false;

                if (reg.Status == RegistrationStatus.Confirmed) return true;
                if (reg.Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected) return false;

                var eventRef = _firestoreDb.Collection("events").Document(reg.EventId);
                var eventSnapshot = await transaction.GetSnapshotAsync(eventRef);
                if (!eventSnapshot.Exists) return false;

                var evDict = eventSnapshot.ToDictionary();
                var evCapacity = evDict.TryGetValue("capacity", out var cap) && cap is long capLong ? (int)capLong : 0;

                var regsQuery = _firestoreDb.Collection(CollectionName)
                    .WhereEqualTo("tenantId", tenantId)
                    .WhereEqualTo("eventId", reg.EventId);
                var regsSnapshot = await transaction.GetSnapshotAsync(regsQuery);

                var existing = regsSnapshot.Documents
                    .Select(MapToRegistration)
                    .ToList();

                var activeCount = existing.Count(r => r.Id != registrationId && ActiveStatuses.Contains(r.Status));

                if (evCapacity > 0 && activeCount >= evCapacity)
                {
                    _logger.LogWarning($"Cannot approve registration {registrationId} - Event capacity {evCapacity} reached");
                    return false;
                }

                reg.Status = RegistrationStatus.Confirmed;
                reg.ProcessedById = processedById;
                reg.ProcessedAt = DateTime.UtcNow;
                reg.RejectionReason = null;
                reg.UpdatedAt = DateTime.UtcNow;

                transaction.Set(regRef, reg.ToFirestoreDocument(), SetOptions.MergeAll);
                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error approving registration {registrationId}");
            return false;
        }
    }

    public async Task<bool> RejectAsync(string registrationId, string tenantId, string processedById, string reason)
    {
        try
        {
            return await _firestoreDb.RunTransactionAsync<bool>(async transaction =>
            {
                var regRef = _firestoreDb.Collection(CollectionName).Document(registrationId);
                var snapshot = await transaction.GetSnapshotAsync(regRef);
                if (!snapshot.Exists) return false;

                var reg = MapToRegistration(snapshot);
                if (reg.TenantId != tenantId) return false;

                if (reg.Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected)
                    return false;

                var freedSeat = ActiveStatuses.Contains(reg.Status);

                Registration? promotedWaitlist = null;
                var promotedStatus = RegistrationStatus.Confirmed;
                if (freedSeat)
                {
                    promotedWaitlist = await GetWaitlistPromoteTargetInTransactionAsync(transaction, reg.EventId, tenantId);
                    var eventSnapshot = await transaction.GetSnapshotAsync(_firestoreDb.Collection("events").Document(reg.EventId));
                    var eventData = eventSnapshot.Exists ? eventSnapshot.ToDictionary() : null;
                    var eventPrice = eventData != null && eventData.TryGetValue("price", out var price) && price != null
                        ? Convert.ToDecimal(price)
                        : 0m;
                    promotedStatus = eventPrice > 0 ? RegistrationStatus.PendingPayment : RegistrationStatus.Confirmed;
                }

                reg.Status = RegistrationStatus.Rejected;
                reg.ProcessedById = processedById;
                reg.ProcessedAt = DateTime.UtcNow;
                reg.RejectionReason = reason;
                reg.UpdatedAt = DateTime.UtcNow;

                transaction.Set(regRef, reg.ToFirestoreDocument(), SetOptions.MergeAll);

                if (promotedWaitlist != null)
                {
                    promotedWaitlist.Status = promotedStatus;
                    promotedWaitlist.PaymentExpiresAt = promotedStatus == RegistrationStatus.PendingPayment
                        ? DateTime.UtcNow.AddMinutes(5)
                        : null;
                    promotedWaitlist.UpdatedAt = DateTime.UtcNow;
                    var promoteRef = _firestoreDb.Collection(CollectionName).Document(promotedWaitlist.Id);
                    transaction.Set(promoteRef, promotedWaitlist.ToFirestoreDocument(), SetOptions.MergeAll);
                    _logger.LogInformation($"Promoted registration {promotedWaitlist.Id} from waitlist for event {reg.EventId} inside transaction");
                }

                return true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error rejecting registration {registrationId}");
            return false;
        }
    }

    public async Task<bool> MarkAsPaidAsync(string registrationId, string tenantId, string transactionId, decimal amount, decimal platformFeePercentage)
    {
        try
        {
            var reg = await GetRegistrationByIdAsync(registrationId, tenantId);
            if (reg == null) return false;

            if (reg.IsPaid)
                return reg.PaymentTransactionId == transactionId;

            if (reg.Status != RegistrationStatus.PendingPayment ||
                (reg.PaymentExpiresAt.HasValue && reg.PaymentExpiresAt.Value < DateTime.UtcNow))
                return false;

            var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
            if (ev == null || ev.Price != amount)
                return false;

            reg.IsPaid = true;
            reg.PaymentTransactionId = transactionId;
            reg.PaymentDate = DateTime.UtcNow;
            reg.PaymentExpiresAt = null;
            reg.Status = RegistrationStatus.Approved;

            decimal fee = amount * platformFeePercentage / 100m;
            reg.PlatformFee = fee;
            reg.OrganizerRevenue = amount - fee;

            return await SaveAsync(reg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking registration {registrationId} as paid");
            return false;
        }
    }

    public async Task<int> ExpirePendingPaymentsAsync()
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("status", (int)RegistrationStatus.PendingPayment)
                .GetSnapshotAsync();

            var expired = snapshot.Documents
                .Select(MapToRegistration)
                .Where(registration => registration.PaymentExpiresAt.HasValue && registration.PaymentExpiresAt.Value <= DateTime.UtcNow)
                .ToList();

            var expiredCount = 0;
            foreach (var registration in expired)
            {
                if (await CancelAsync(registration.Id, registration.TenantId))
                    expiredCount++;
            }

            return expiredCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring pending payments");
            return 0;
        }
    }

    public async Task<(Registration? Registration, string? Error)> GenerateCheckInCodeAsync(string eventId, string tenantId, string userId)
    {
        try
        {
            var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
            if (ev == null) return (null, "Event not found");

            var registrations = await GetRegistrationsByEventAsync(eventId, tenantId);
            var reg = registrations.FirstOrDefault(r => r.UserId == userId);
            if (reg == null) return (null, "You are not registered for this event");

            // Only confirmed attendees get a check-in code.
            if (reg.Status != RegistrationStatus.Confirmed)
                return (null, "Registration is not confirmed");

            string code = string.Empty;
            bool isUnique = false;
            int retries = 0;
            do
            {
                code = GenerateCode();
                var dupSnapshot = await _firestoreDb.Collection(CollectionName)
                    .WhereEqualTo("tenantId", tenantId)
                    .WhereEqualTo("checkInCode", code)
                    .GetSnapshotAsync();

                if (dupSnapshot.Documents.Count == 0)
                {
                    isUnique = true;
                }
                retries++;
            } while (!isUnique && retries < 5);

            if (!isUnique)
                return (null, "Failed to generate a unique check-in code");

            reg.CheckInCode = code;
            // Code stays valid until the event ends.
            reg.CheckInCodeExpiresAt = ev.EndTime.ToUniversalTime();

            var success = await SaveAsync(reg);
            if (!success) return (null, "Failed to generate check-in code");

            return (reg, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating check-in code for user {userId} event {eventId}");
            return (null, "Failed to generate check-in code");
        }
    }

    public async Task<(Registration? Registration, string? Error)> ValidateCheckInAsync(
        string code, string tenantId, string requesterUserId, bool requesterIsAdminOrManager)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, "Check-in code is required");

        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("checkInCode", code)
                .GetSnapshotAsync();

            var reg = snapshot.Documents.Select(MapToRegistration).FirstOrDefault();
            if (reg == null) return (null, "Invalid check-in code");

            // Only the event's organizer or an admin/manager may check attendees in.
            if (!requesterIsAdminOrManager)
            {
                var ev = await _eventService.GetEventByIdAsync(reg.EventId, tenantId);
                if (ev == null || ev.OrganizerId != requesterUserId)
                    return (null, IRegistrationService.ForbiddenError);
            }

            if (reg.Status != RegistrationStatus.Confirmed)
                return (null, "Registration is not confirmed");

            if (reg.CheckInCodeExpiresAt.HasValue && reg.CheckInCodeExpiresAt.Value < DateTime.UtcNow)
                return (null, "Check-in code has expired");

            if (reg.CheckedIn)
                return (null, "Already checked in");

            reg.CheckedIn = true;
            reg.CheckedInAt = DateTime.UtcNow;

            var success = await SaveAsync(reg);
            if (!success) return (null, "Failed to check in");

            _logger.LogInformation($"Checked in registration {reg.Id} (event {reg.EventId}, user {reg.UserId})");
            return (reg, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating check-in code");
            return (null, "Failed to check in");
        }
    }

    // Short, human-typable, unique-enough code (uppercased hex from a GUID).
    private static string GenerateCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    // Retrieves the earliest-registered waitlisted entry to Pending when a seat frees up inside a transaction.
    private async Task<Registration?> GetWaitlistPromoteTargetInTransactionAsync(Transaction transaction, string eventId, string tenantId)
    {
        var regsQuery = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo("tenantId", tenantId)
            .WhereEqualTo("eventId", eventId)
            .WhereEqualTo("status", (int)RegistrationStatus.Waitlisted);
        var snapshot = await transaction.GetSnapshotAsync(regsQuery);

        var waitlisted = snapshot.Documents
            .Select(MapToRegistration)
            .OrderBy(r => r.RegisteredAt)
            .FirstOrDefault();

        return waitlisted;
    }

    private async Task<bool> SaveAsync(Registration reg)
    {
        try
        {
            reg.UpdatedAt = DateTime.UtcNow;
            var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
            await docRef.SetAsync(reg.ToFirestoreDocument(), SetOptions.MergeAll);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving registration {reg.Id}");
            return false;
        }
    }

    // =========================================================================
    // MVC Student Portal compatibility methods
    // =========================================================================

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
        return await GetRegistrationsByEventAsync(eventId, tenantId, null);
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
            if (existingReg.Status is RegistrationStatus.Approved or RegistrationStatus.Pending or RegistrationStatus.Confirmed)
            {
                throw new BusinessRuleException("Bạn đã đăng ký tham gia sự kiện này rồi.");
            }
        }

        // 5. Check Capacity
        var eventRegs = await GetRegistrationsByEventAsync(eventId, tenantId);
        var activeCount = eventRegs.Count(r => ActiveStatuses.Contains(r.Status));
        var isFull = ev.Capacity > 0 && activeCount >= ev.Capacity;

        // 5.5 Find user ID by email
        string userId = string.Empty;
        try
        {
            var userQuery = _firestoreDb.Collection("users")
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("email", studentEmail)
                .Limit(1);
            var userSnapshot = await userQuery.GetSnapshotAsync();
            if (userSnapshot.Documents.Count > 0)
            {
                userId = userSnapshot.Documents[0].Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to resolve userId for student email {studentEmail} during registration");
        }

        // 6. Create or update registration
        var reg = existingReg ?? new Registration
        {
            TenantId = tenantId,
            EventId = eventId,
            StudentEmail = studentEmail,
            StudentName = studentName
        };

        if (string.IsNullOrEmpty(reg.UserId) && !string.IsNullOrEmpty(userId))
        {
            reg.UserId = userId;
        }

        if (isFull)
        {
            reg.Status = RegistrationStatus.Waitlisted;
            reg.PaymentExpiresAt = null;
        }
        else if (!ev.IsFree && ev.Price > 0)
        {
            reg.Status = RegistrationStatus.PendingPayment;
            reg.PaymentExpiresAt = DateTime.UtcNow.AddMinutes(5);
        }
        else
        {
            reg.Status = RegistrationStatus.Approved; // Automatically approve free registrations
            reg.PaymentExpiresAt = null;
        }

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

    public async Task<(bool Success, string Message)> CheckInAsync(string tenantId, string eventId, string studentEmail, string checkInCode)
    {
        try
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

            // 3. Validate mã check-in
            if (string.IsNullOrWhiteSpace(ev.CheckInCode))
                return (false, "Sự kiện chưa có mã check-in. Vui lòng liên hệ ban tổ chức.");

            if (!string.Equals(ev.CheckInCode.Trim(), checkInCode.Trim(), StringComparison.OrdinalIgnoreCase))
                return (false, "Mã check-in không đúng. Vui lòng kiểm tra lại.");

            if (ev.CheckInCodeExpiresAt.HasValue && now > ev.CheckInCodeExpiresAt.Value)
                return (false, "Mã check-in đã hết hạn. Vui lòng xin mã mới từ ban tổ chức.");

            // 4. Tìm đăng ký hợp lệ
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("eventId", eventId)
                .WhereEqualTo("studentEmail", studentEmail)
                .GetSnapshotAsync();

            var regDoc = snapshot.Documents.FirstOrDefault();
            if (regDoc == null)
                return (false, "Bạn chưa đăng ký tham gia sự kiện này.");

            var reg = MapToRegistration(regDoc);

            if (reg.Status != RegistrationStatus.Approved)
                return (false, "Đăng ký của bạn chưa được phê duyệt.");

            if (reg.CheckedIn)
                return (false, $"Bạn đã check-in sự kiện này lúc {reg.CheckedInAt?.ToLocalTime().ToString("HH:mm dd/MM/yyyy")}.");

            // 5. Cập nhật Firestore
            reg.CheckedIn = true;
            reg.CheckedInAt = now;
            reg.UpdatedAt = now;

            var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
            await docRef.SetAsync(reg.ToFirestoreDocument(), SetOptions.MergeAll);

            _logger.LogInformation($"Check-in successful: student {studentEmail}, event {eventId}");
            return (true, $"Check-in thành công! Chào mừng bạn đến với \"{ev.Title}\".");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during check-in for student {studentEmail}, event {eventId}");
            return (false, "Đã xảy ra lỗi hệ thống khi check-in. Vui lòng thử lại sau.");
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
            UserId = dict.TryGetValue("userId", out var uid) ? uid?.ToString() ?? "" : "",
            Note = dict.TryGetValue("note", out var note) && !string.IsNullOrEmpty(note?.ToString()) ? note!.ToString() : null,
            Status = dict.TryGetValue("status", out var st) && st is long stLong ? (RegistrationStatus)(int)stLong : RegistrationStatus.Pending,
            RegisteredAt = dict.TryGetValue("registeredAt", out var reg) && reg is Timestamp regTs ? regTs.ToDateTime() : DateTime.UtcNow,
            ProcessedById = dict.TryGetValue("processedById", out var pid) && !string.IsNullOrEmpty(pid?.ToString()) ? pid!.ToString() : null,
            ProcessedAt = dict.TryGetValue("processedAt", out var pAt) && pAt is Timestamp pAtTs && pAtTs.ToDateTime() != DateTime.MinValue ? pAtTs.ToDateTime() : null,
            RejectionReason = dict.TryGetValue("rejectionReason", out var rej) && !string.IsNullOrEmpty(rej?.ToString()) ? rej!.ToString() : null,
            CancelledAt = dict.TryGetValue("cancelledAt", out var cAt) && cAt is Timestamp cAtTs && cAtTs.ToDateTime() != DateTime.MinValue ? cAtTs.ToDateTime() : null,
            CheckInCode = dict.TryGetValue("checkInCode", out var code) && !string.IsNullOrEmpty(code?.ToString()) ? code!.ToString() : null,
            CheckInCodeExpiresAt = dict.TryGetValue("checkInCodeExpiresAt", out var exp) && exp is Timestamp expTs && expTs.ToDateTime() != DateTime.MinValue ? expTs.ToDateTime() : null,
            CheckedIn = dict.TryGetValue("checkedIn", out var ci) && ci is bool ciBool && ciBool,
            CheckedInAt = dict.TryGetValue("checkedInAt", out var ciAt) && ciAt is Timestamp ciAtTs && ciAtTs.ToDateTime() != DateTime.MinValue ? ciAtTs.ToDateTime() : null,
            ReminderSent = dict.TryGetValue("reminderSent", out var rs) && rs is bool rsBool && rsBool,
            ReminderSentAt = dict.TryGetValue("reminderSentAt", out var rsAt) && rsAt is Timestamp rsAtTs && rsAtTs.ToDateTime() != DateTime.MinValue ? rsAtTs.ToDateTime() : null,
            StudentEmail = dict.TryGetValue("studentEmail", out var email) ? email?.ToString() ?? "" : "",
            StudentName = dict.TryGetValue("studentName", out var name) ? name?.ToString() ?? "" : "",
            IsPaid = dict.TryGetValue("isPaid", out var paid) && paid is bool paidBool && paidBool,
            PaymentTransactionId = dict.TryGetValue("paymentTransactionId", out var ptId) ? ptId?.ToString() : null,
            PaymentDate = dict.TryGetValue("paymentDate", out var pDate) && pDate is Timestamp pDateTs && pDateTs.ToDateTime() != DateTime.MinValue ? pDateTs.ToDateTime() : null,
            PaymentExpiresAt = dict.TryGetValue("paymentExpiresAt", out var pExpires) && pExpires is Timestamp pExpiresTs && pExpiresTs.ToDateTime() != DateTime.MinValue ? pExpiresTs.ToDateTime() : null,
            PlatformFee = dict.TryGetValue("platformFee", out var pf) && pf is double pfDouble ? (decimal)pfDouble : 0,
            OrganizerRevenue = dict.TryGetValue("organizerRevenue", out var or) && or is double orDouble ? (decimal)orDouble : 0,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }
}
