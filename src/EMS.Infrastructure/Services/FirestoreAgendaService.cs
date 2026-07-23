using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMS.Core.Entities;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

public class FirestoreAgendaService : IAgendaService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreAgendaService> _logger;
    private const string CollectionName = "agendaItems";

    public FirestoreAgendaService(FirestoreDb firestoreDb, ILogger<FirestoreAgendaService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<List<AgendaItem>> GetAgendaByEventAsync(string eventId, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName)
                .WhereEqualTo("tenantId", tenantId)
                .WhereEqualTo("eventId", eventId)
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(MapToAgendaItem)
                .OrderBy(a => a.StartTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting agenda for event {eventId}");
            return new List<AgendaItem>();
        }
    }

    public async Task<AgendaItem?> GetAgendaItemByIdAsync(string id, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var item = MapToAgendaItem(snapshot);
            if (item.TenantId != tenantId) return null;

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting agenda item by id {id}");
            return null;
        }
    }

    public async Task<AgendaItem?> CreateAgendaItemAsync(AgendaItem item)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(item.Id);
            await docRef.SetAsync(item.ToFirestoreDocument());
            _logger.LogInformation($"Agenda item created: {item.Id} for event {item.EventId}");
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating agenda item {item.Id}");
            return null;
        }
    }

    public async Task<bool> UpdateAgendaItemAsync(AgendaItem item)
    {
        try
        {
            item.UpdatedAt = DateTime.UtcNow;
            var docRef = _firestoreDb.Collection(CollectionName).Document(item.Id);
            await docRef.SetAsync(item.ToFirestoreDocument(), SetOptions.MergeAll);
            _logger.LogInformation($"Agenda item updated: {item.Id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating agenda item {item.Id}");
            return false;
        }
    }

    public async Task<bool> DeleteAgendaItemAsync(string id, string tenantId)
    {
        try
        {
            var existing = await GetAgendaItemByIdAsync(id, tenantId);
            if (existing == null) return false;

            await _firestoreDb.Collection(CollectionName).Document(id).DeleteAsync();
            _logger.LogInformation($"Agenda item deleted: {id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting agenda item {id}");
            return false;
        }
    }

    private static AgendaItem MapToAgendaItem(DocumentSnapshot snapshot)
    {
        var dict = snapshot.ToDictionary();
        return new AgendaItem
        {
            Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? snapshot.Id : snapshot.Id,
            TenantId = dict.TryGetValue("tenantId", out var tid) ? tid?.ToString() ?? "" : "",
            EventId = dict.TryGetValue("eventId", out var eid) ? eid?.ToString() ?? "" : "",
            StartTime = dict.TryGetValue("startTime", out var start) && start is Timestamp startTs ? startTs.ToDateTime() : DateTime.MinValue,
            EndTime = dict.TryGetValue("endTime", out var end) && end is Timestamp endTs ? endTs.ToDateTime() : DateTime.MinValue,
            Title = dict.TryGetValue("title", out var title) ? title?.ToString() ?? "" : "",
            CreatedAt = dict.TryGetValue("createdAt", out var created) && created is Timestamp createdTs ? createdTs.ToDateTime() : DateTime.UtcNow,
            UpdatedAt = dict.TryGetValue("updatedAt", out var updated) && updated is Timestamp updatedTs ? updatedTs.ToDateTime() : DateTime.UtcNow
        };
    }
}
