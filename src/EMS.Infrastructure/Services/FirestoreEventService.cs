using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

public class FirestoreEventService : IEventService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreEventService> _logger;
    private const string CollectionName = "events";

    public FirestoreEventService(FirestoreDb firestoreDb, ILogger<FirestoreEventService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<Event?> GetEventByIdAsync(string eventId, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).Document(eventId).GetSnapshotAsync();

            if (!snapshot.Exists) return null;

            var ev = MapToEvent(snapshot);
            // Tenant isolation: never leak events across tenants.
            if (ev.TenantId != tenantId) return null;

            return ev;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting event by id {eventId}");
            return null;
        }
    }

    public async Task<List<Event>> GetEventsByTenantAsync(string tenantId, EventStatus? status = null)
    {
        try
        {
            Query query = _firestoreDb.Collection(CollectionName).WhereEqualTo("tenantId", tenantId);

            if (status.HasValue)
            {
                query = query.WhereEqualTo("status", (int)status.Value);
            }

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToEvent)
                .OrderByDescending(e => e.StartTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting events for tenant {tenantId}");
            return new List<Event>();
        }
    }

    public async Task<Event?> CreateEventAsync(Event ev)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(ev.Id);
            await docRef.SetAsync(ev.ToFirestoreDocument());

            _logger.LogInformation($"Event created: {ev.Id} ({ev.Title})");
            return ev;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating event {ev.Id}");
            return null;
        }
    }

    public async Task<bool> UpdateEventAsync(Event ev)
    {
        try
        {
            ev.UpdatedAt = DateTime.UtcNow;
            var docRef = _firestoreDb.Collection(CollectionName).Document(ev.Id);
            await docRef.SetAsync(ev.ToFirestoreDocument(), SetOptions.MergeAll);

            _logger.LogInformation($"Event updated: {ev.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating event {ev.Id}");
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(string eventId, string tenantId)
    {
        try
        {
            // Confirm ownership before deleting to enforce tenant isolation.
            var existing = await GetEventByIdAsync(eventId, tenantId);
            if (existing == null) return false;

            await _firestoreDb.Collection(CollectionName).Document(eventId).DeleteAsync();

            _logger.LogInformation($"Event deleted: {eventId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting event {eventId}");
            return false;
        }
    }

    public async Task<bool> ApproveEventAsync(string eventId, string tenantId, string approvedById)
    {
        var ev = await GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return false;

        ev.Status = EventStatus.Approved;
        ev.ApprovedById = approvedById;
        ev.ApprovedAt = DateTime.UtcNow;
        ev.RejectionReason = null;

        return await UpdateEventAsync(ev);
    }

    public async Task<bool> RejectEventAsync(string eventId, string tenantId, string approvedById, string reason)
    {
        var ev = await GetEventByIdAsync(eventId, tenantId);
        if (ev == null) return false;

        ev.Status = EventStatus.Rejected;
        ev.ApprovedById = approvedById;
        ev.ApprovedAt = DateTime.UtcNow;
        ev.RejectionReason = reason;

        return await UpdateEventAsync(ev);
    }

    private static Event MapToEvent(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();

        return new Event
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            TenantId = dict.TryGetValue("tenantId", out var tid) ? tid?.ToString() ?? "" : "",
            Title = dict.TryGetValue("title", out var title) ? title?.ToString() ?? "" : "",
            Description = dict.TryGetValue("description", out var desc) ? desc?.ToString() ?? "" : "",
            Location = dict.TryGetValue("location", out var loc) ? loc?.ToString() ?? "" : "",
            VenueId = dict.TryGetValue("venueId", out var venue) && !string.IsNullOrEmpty(venue?.ToString()) ? venue!.ToString() : null,
            StartTime = dict.TryGetValue("startTime", out var start) && start is Timestamp startTs ? startTs.ToDateTime() : DateTime.MinValue,
            EndTime = dict.TryGetValue("endTime", out var end) && end is Timestamp endTs ? endTs.ToDateTime() : DateTime.MinValue,
            Capacity = dict.TryGetValue("capacity", out var cap) && cap is long capLong ? (int)capLong : 0,
            ImageUrl = dict.TryGetValue("imageUrl", out var img) && !string.IsNullOrEmpty(img?.ToString()) ? img!.ToString() : null,
            OrganizerId = dict.TryGetValue("organizerId", out var org) ? org?.ToString() ?? "" : "",
            Status = dict.TryGetValue("status", out var st) && st is long stLong ? (EventStatus)(int)stLong : EventStatus.Pending,
            ApprovedById = dict.TryGetValue("approvedById", out var appBy) && !string.IsNullOrEmpty(appBy?.ToString()) ? appBy!.ToString() : null,
            ApprovedAt = dict.TryGetValue("approvedAt", out var appAt) && appAt is Timestamp appAtTs && appAtTs.ToDateTime() != DateTime.MinValue ? appAtTs.ToDateTime() : null,
            RejectionReason = dict.TryGetValue("rejectionReason", out var rej) && !string.IsNullOrEmpty(rej?.ToString()) ? rej!.ToString() : null,
            CheckInCode = dict.TryGetValue("checkInCode", out var cic) && !string.IsNullOrEmpty(cic?.ToString()) ? cic!.ToString() : null,
            CheckInCodeExpiresAt = dict.TryGetValue("checkInCodeExpiresAt", out var cice) && cice is Timestamp ciceTs && ciceTs.ToDateTime() != DateTime.MinValue ? ciceTs.ToDateTime() : null,
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }
}
