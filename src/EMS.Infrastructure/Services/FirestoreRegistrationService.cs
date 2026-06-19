using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
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

    // Statuses that count against an event's capacity (i.e. an active seat is taken/held).
    private static readonly RegistrationStatus[] ActiveStatuses =
    {
        RegistrationStatus.Pending,
        RegistrationStatus.Confirmed
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
            var ev = await _eventService.GetEventByIdAsync(eventId, tenantId);
            if (ev == null) return (null, "Event not found");

            // Only approved events accept registrations.
            if (ev.Status != EventStatus.Approved)
                return (null, "Event is not open for registration");

            var existing = await GetRegistrationsByEventAsync(eventId, tenantId);

            // Block duplicate registration while the user already holds an active/waitlisted seat.
            if (existing.Any(r => r.UserId == userId &&
                                  r.Status is RegistrationStatus.Pending
                                           or RegistrationStatus.Confirmed
                                           or RegistrationStatus.Waitlisted))
            {
                return (null, "You are already registered for this event");
            }

            // Capacity 0 (or negative) is treated as unlimited.
            var activeCount = existing.Count(r => ActiveStatuses.Contains(r.Status));
            var isFull = ev.Capacity > 0 && activeCount >= ev.Capacity;

            var reg = new Registration
            {
                TenantId = tenantId,
                EventId = eventId,
                UserId = userId,
                Note = note,
                Status = isFull ? RegistrationStatus.Waitlisted : RegistrationStatus.Pending
            };

            var docRef = _firestoreDb.Collection(CollectionName).Document(reg.Id);
            await docRef.SetAsync(reg.ToFirestoreDocument());

            _logger.LogInformation($"Registration created: {reg.Id} (event {eventId}, user {userId}, status {reg.Status})");
            return (reg, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering user {userId} for event {eventId}");
            return (null, "Failed to register");
        }
    }

    public async Task<bool> CancelAsync(string registrationId, string tenantId)
    {
        var reg = await GetRegistrationByIdAsync(registrationId, tenantId);
        if (reg == null) return false;

        // Already cancelled/rejected registrations are terminal.
        if (reg.Status is RegistrationStatus.Cancelled or RegistrationStatus.Rejected)
            return false;

        var freedSeat = ActiveStatuses.Contains(reg.Status);

        reg.Status = RegistrationStatus.Cancelled;
        reg.CancelledAt = DateTime.UtcNow;

        var success = await SaveAsync(reg);
        if (!success) return false;

        // If the cancelled registration was holding a seat, promote the earliest waitlisted one.
        if (freedSeat)
        {
            await PromoteFromWaitlistAsync(reg.EventId, tenantId);
        }

        return true;
    }

    public async Task<bool> ApproveAsync(string registrationId, string tenantId, string processedById)
    {
        var reg = await GetRegistrationByIdAsync(registrationId, tenantId);
        if (reg == null) return false;

        reg.Status = RegistrationStatus.Confirmed;
        reg.ProcessedById = processedById;
        reg.ProcessedAt = DateTime.UtcNow;
        reg.RejectionReason = null;

        return await SaveAsync(reg);
    }

    public async Task<bool> RejectAsync(string registrationId, string tenantId, string processedById, string reason)
    {
        var reg = await GetRegistrationByIdAsync(registrationId, tenantId);
        if (reg == null) return false;

        var freedSeat = ActiveStatuses.Contains(reg.Status);

        reg.Status = RegistrationStatus.Rejected;
        reg.ProcessedById = processedById;
        reg.ProcessedAt = DateTime.UtcNow;
        reg.RejectionReason = reason;

        var success = await SaveAsync(reg);
        if (!success) return false;

        if (freedSeat)
        {
            await PromoteFromWaitlistAsync(reg.EventId, tenantId);
        }

        return true;
    }

    // Promotes the earliest-registered waitlisted entry to Pending when a seat frees up.
    private async Task PromoteFromWaitlistAsync(string eventId, string tenantId)
    {
        try
        {
            var waitlisted = await GetRegistrationsByEventAsync(eventId, tenantId, RegistrationStatus.Waitlisted);
            var next = waitlisted.OrderBy(r => r.RegisteredAt).FirstOrDefault();
            if (next == null) return;

            next.Status = RegistrationStatus.Pending;
            await SaveAsync(next);

            _logger.LogInformation($"Promoted registration {next.Id} from waitlist for event {eventId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error promoting waitlist for event {eventId}");
        }
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
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }
}
