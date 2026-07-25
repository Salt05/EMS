using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services;

public class FirestoreRewardService : IRewardCategoryService, IEventRewardService, IUserRewardService
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<FirestoreRewardService> _logger;

    private const string CategoriesCollection = "reward_categories";
    private const string EventRewardsCollection = "event_rewards";
    private const string UserRecordsCollection = "user_reward_records";

    public FirestoreRewardService(FirestoreDb firestoreDb, ILogger<FirestoreRewardService> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    #region IRewardCategoryService
    public async Task<List<RewardCategory>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true)
    {
        try
        {
            Query query = _firestoreDb.Collection(CategoriesCollection);

            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            if (activeOnly)
            {
                query = query.WhereEqualTo("isActive", true);
            }

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(MapToCategory).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting categories for tenant {tenantId}");
            return new List<RewardCategory>();
        }
    }

    public async Task<RewardCategory?> GetCategoryByIdAsync(string id, string tenantId)
    {
        try
        {
            var snapshot = await _firestoreDb.Collection(CategoriesCollection).Document(id).GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            var cat = MapToCategory(snapshot);
            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase) && cat.TenantId != tenantId)
            {
                return null;
            }
            return cat;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting category by id {id}");
            return null;
        }
    }

    public async Task<RewardCategory> CreateCategoryAsync(RewardCategory category)
    {
        try
        {
            if (string.IsNullOrEmpty(category.Id)) category.Id = Guid.NewGuid().ToString();
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            var docRef = _firestoreDb.Collection(CategoriesCollection).Document(category.Id);
            var data = new Dictionary<string, object>
            {
                { "id", category.Id },
                { "tenantId", category.TenantId ?? "default" },
                { "name", category.Name },
                { "type", (int)category.Type },
                { "description", category.Description ?? "" },
                { "isActive", category.IsActive },
                { "createdAt", category.CreatedAt.ToUniversalTime() },
                { "updatedAt", category.UpdatedAt.ToUniversalTime() }
            };

            await docRef.SetAsync(data);
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating category {category.Name}");
            return category;
        }
    }

    public async Task<bool> UpdateCategoryAsync(RewardCategory category)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CategoriesCollection).Document(category.Id);
            var data = new Dictionary<string, object>
            {
                { "name", category.Name },
                { "type", (int)category.Type },
                { "description", category.Description ?? "" },
                { "isActive", category.IsActive },
                { "updatedAt", DateTime.UtcNow.ToUniversalTime() }
            };

            await docRef.UpdateAsync(data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating category {category.Id}");
            return false;
        }
    }

    public async Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive)
    {
        try
        {
            var docRef = _firestoreDb.Collection(CategoriesCollection).Document(id);
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { "isActive", isActive },
                { "updatedAt", DateTime.UtcNow.ToUniversalTime() }
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error toggling active status for category {id}");
            return false;
        }
    }

    private static RewardCategory MapToCategory(DocumentSnapshot doc)
    {
        return new RewardCategory
        {
            Id = doc.GetValue<string>("id"),
            TenantId = doc.ContainsField("tenantId") ? doc.GetValue<string>("tenantId") : "default",
            Name = doc.GetValue<string>("name"),
            Type = (RewardType)doc.GetValue<int>("type"),
            Description = doc.ContainsField("description") ? doc.GetValue<string>("description") : "",
            IsActive = doc.ContainsField("isActive") && doc.GetValue<bool>("isActive"),
            CreatedAt = doc.ContainsField("createdAt") ? doc.GetValue<Timestamp>("createdAt").ToDateTime() : DateTime.UtcNow,
            UpdatedAt = doc.ContainsField("updatedAt") ? doc.GetValue<Timestamp>("updatedAt").ToDateTime() : DateTime.UtcNow
        };
    }
    #endregion

    #region IEventRewardService
    public async Task<List<EventReward>> GetRewardsByEventAsync(string eventId, string tenantId)
    {
        try
        {
            _logger.LogInformation($"[FirestoreRewardService] Querying rewards for eventId={eventId}");
            Query query = _firestoreDb.Collection(EventRewardsCollection).WhereEqualTo("eventId", eventId);

            var snapshot = await query.GetSnapshotAsync();
            _logger.LogInformation($"[FirestoreRewardService] Found {snapshot.Documents.Count} reward document(s) for eventId={eventId}");
            return snapshot.Documents.Select(MapToEventReward).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[FirestoreRewardService] Error getting rewards for event {eventId}");
            return new List<EventReward>();
        }
    }

    public async Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventReward> rewards)
    {
        try
        {
            // First, delete existing rewards for this event
            var existingSnapshot = await _firestoreDb.Collection(EventRewardsCollection).WhereEqualTo("eventId", eventId).GetSnapshotAsync();
            var batch = _firestoreDb.StartBatch();
            foreach (var doc in existingSnapshot.Documents)
            {
                batch.Delete(doc.Reference);
            }

            // Add new rewards
            foreach (var r in rewards)
            {
                var id = string.IsNullOrEmpty(r.Id) ? Guid.NewGuid().ToString() : r.Id;
                var docRef = _firestoreDb.Collection(EventRewardsCollection).Document(id);
                var data = new Dictionary<string, object>
                {
                    { "id", id },
                    { "eventId", eventId },
                    { "tenantId", string.IsNullOrEmpty(r.TenantId) ? (string.IsNullOrEmpty(tenantId) ? "default" : tenantId) : r.TenantId },
                    { "rewardCategoryId", r.RewardCategoryId ?? "" },
                    { "detailName", r.DetailName ?? "" },
                    { "valueOrQuantity", r.ValueOrQuantity },
                    { "description", r.Description ?? "" },
                    { "createdAt", DateTime.UtcNow.ToUniversalTime() },
                    { "updatedAt", DateTime.UtcNow.ToUniversalTime() }
                };
                batch.Set(docRef, data);
            }

            await batch.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving event rewards for event {eventId}");
            return false;
        }
    }

    public async Task<bool> DeleteRewardAsync(string rewardId, string tenantId)
    {
        try
        {
            await _firestoreDb.Collection(EventRewardsCollection).Document(rewardId).DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting reward {rewardId}");
            return false;
        }
    }

    private static EventReward MapToEventReward(DocumentSnapshot doc)
    {
        try
        {
            return new EventReward
            {
                Id = GetStringValue(doc, "id", doc.Id),
                EventId = GetStringValue(doc, "eventId", ""),
                TenantId = GetStringValue(doc, "tenantId", "default"),
                RewardCategoryId = GetStringValue(doc, "rewardCategoryId", ""),
                DetailName = GetStringValue(doc, "detailName", ""),
                ValueOrQuantity = GetDoubleValue(doc, "valueOrQuantity"),
                Description = GetStringValue(doc, "description", ""),
                CreatedAt = GetDateTimeValue(doc, "createdAt"),
                UpdatedAt = GetDateTimeValue(doc, "updatedAt")
            };
        }
        catch
        {
            return new EventReward { Id = doc.Id, EventId = GetStringValue(doc, "eventId", "") };
        }
    }

    private static string GetStringValue(DocumentSnapshot doc, string fieldName, string defaultValue = "")
    {
        if (!doc.ContainsField(fieldName)) return defaultValue;
        var val = doc.GetValue<object>(fieldName);
        return val?.ToString() ?? defaultValue;
    }

    private static double GetDoubleValue(DocumentSnapshot doc, string fieldName, double defaultValue = 0)
    {
        if (!doc.ContainsField(fieldName)) return defaultValue;
        var val = doc.GetValue<object>(fieldName);
        if (val == null) return defaultValue;
        if (val is double d) return d;
        if (val is float f) return f;
        if (val is long l) return (double)l;
        if (val is int i) return (double)i;
        if (double.TryParse(val.ToString(), out var parsed)) return parsed;
        return defaultValue;
    }

    private static DateTime GetDateTimeValue(DocumentSnapshot doc, string fieldName)
    {
        if (!doc.ContainsField(fieldName)) return DateTime.UtcNow;
        try
        {
            var val = doc.GetValue<object>(fieldName);
            if (val == null) return DateTime.UtcNow;
            if (val is Timestamp ts) return ts.ToDateTime();
            if (val is DateTime dt) return dt;
            if (DateTime.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        catch { }
        return DateTime.UtcNow;
    }
    #endregion

    #region IUserRewardService
    public async Task<bool> GrantRewardsOnCheckInAsync(string tenantId, string eventId, string userId, string studentEmail, string studentName)
    {
        try
        {
            // Check if already granted
            var existingQuery = await _firestoreDb.Collection(UserRecordsCollection)
                .WhereEqualTo("eventId", eventId)
                .WhereEqualTo("studentEmail", studentEmail.ToLowerInvariant())
                .GetSnapshotAsync();

            if (existingQuery.Documents.Any()) return true;

            // Fetch rewards for event
            var eventRewards = await GetRewardsByEventAsync(eventId, tenantId);
            if (!eventRewards.Any()) return true;

            var batch = _firestoreDb.StartBatch();
            foreach (var er in eventRewards)
            {
                var cat = await GetCategoryByIdAsync(er.RewardCategoryId, tenantId);
                var id = Guid.NewGuid().ToString();
                var docRef = _firestoreDb.Collection(UserRecordsCollection).Document(id);
                var data = new Dictionary<string, object>
                {
                    { "id", id },
                    { "tenantId", tenantId },
                    { "userId", userId },
                    { "studentEmail", studentEmail.ToLowerInvariant() },
                    { "studentName", studentName },
                    { "eventId", eventId },
                    { "eventTitle", "Sự kiện #" + eventId },
                    { "rewardCategoryId", er.RewardCategoryId },
                    { "rewardCategoryName", cat?.Name ?? "Phần thưởng" },
                    { "rewardType", (int)(cat?.Type ?? RewardType.TrainingPoint) },
                    { "detailName", er.DetailName },
                    { "amount", er.ValueOrQuantity },
                    { "grantedAt", DateTime.UtcNow.ToUniversalTime() }
                };
                batch.Set(docRef, data);
            }

            await batch.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error granting rewards for event {eventId} to user {studentEmail}");
            return false;
        }
    }

    public async Task<List<UserRewardRecord>> GetUserRewardsAsync(string studentEmail, string tenantId, RewardType? type = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            Query query = _firestoreDb.Collection(UserRecordsCollection).WhereEqualTo("studentEmail", studentEmail.ToLowerInvariant());

            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            if (type.HasValue)
            {
                query = query.WhereEqualTo("rewardType", (int)type.Value);
            }

            var snapshot = await query.GetSnapshotAsync();
            var list = snapshot.Documents.Select(MapToUserRecord).ToList();

            if (fromDate.HasValue)
            {
                list = list.Where(r => r.GrantedAt >= fromDate.Value).ToList();
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                list = list.Where(r => r.GrantedAt <= endOfDay).ToList();
            }

            return list.OrderByDescending(r => r.GrantedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user rewards for {studentEmail}");
            return new List<UserRewardRecord>();
        }
    }

    public async Task<List<StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(string tenantId, string? rewardCategoryId = null, RewardType? rewardType = null, DateTime? fromDate = null, DateTime? toDate = null, string sortOrder = "desc", string? searchKeyword = null)
    {
        try
        {
            Query query = _firestoreDb.Collection(UserRecordsCollection);

            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            if (!string.IsNullOrEmpty(rewardCategoryId))
            {
                query = query.WhereEqualTo("rewardCategoryId", rewardCategoryId);
            }

            if (rewardType.HasValue)
            {
                query = query.WhereEqualTo("rewardType", (int)rewardType.Value);
            }

            var snapshot = await query.GetSnapshotAsync();
            var allRecords = snapshot.Documents.Select(MapToUserRecord).ToList();

            if (fromDate.HasValue)
            {
                allRecords = allRecords.Where(r => r.GrantedAt >= fromDate.Value).ToList();
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                allRecords = allRecords.Where(r => r.GrantedAt <= endOfDay).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var kw = searchKeyword.Trim().ToLower();
                allRecords = allRecords.Where(r => r.StudentName.ToLower().Contains(kw) || r.StudentEmail.ToLower().Contains(kw) || r.DetailName.ToLower().Contains(kw)).ToList();
            }

            var grouped = allRecords
                .GroupBy(r => r.StudentEmail.ToLowerInvariant())
                .Select(g => new StudentRewardSummaryDto
                {
                    StudentEmail = g.Key,
                    StudentName = g.First().StudentName,
                    TotalAmount = g.Sum(x => x.Amount),
                    RecordCount = g.Count(),
                    Records = g.OrderByDescending(x => x.GrantedAt).ToList()
                });

            if (sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                return grouped.OrderBy(s => s.TotalAmount).ToList();
            }
            return grouped.OrderByDescending(s => s.TotalAmount).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting reward stats for tenant {tenantId}");
            return new List<StudentRewardSummaryDto>();
        }
    }

    private static UserRewardRecord MapToUserRecord(DocumentSnapshot doc)
    {
        return new UserRewardRecord
        {
            Id = doc.GetValue<string>("id"),
            TenantId = doc.ContainsField("tenantId") ? doc.GetValue<string>("tenantId") : "default",
            UserId = doc.ContainsField("userId") ? doc.GetValue<string>("userId") : "",
            StudentEmail = doc.ContainsField("studentEmail") ? doc.GetValue<string>("studentEmail") : "",
            StudentName = doc.ContainsField("studentName") ? doc.GetValue<string>("studentName") : "",
            EventId = doc.ContainsField("eventId") ? doc.GetValue<string>("eventId") : "",
            EventTitle = doc.ContainsField("eventTitle") ? doc.GetValue<string>("eventTitle") : "",
            RewardCategoryId = doc.ContainsField("rewardCategoryId") ? doc.GetValue<string>("rewardCategoryId") : "",
            RewardCategoryName = doc.ContainsField("rewardCategoryName") ? doc.GetValue<string>("rewardCategoryName") : "",
            RewardType = doc.ContainsField("rewardType") ? (RewardType)doc.GetValue<int>("rewardType") : RewardType.TrainingPoint,
            DetailName = doc.ContainsField("detailName") ? doc.GetValue<string>("detailName") : "",
            Amount = GetDoubleValue(doc, "amount"),
            GrantedAt = doc.ContainsField("grantedAt") ? doc.GetValue<Timestamp>("grantedAt").ToDateTime() : DateTime.UtcNow
        };
    }
    #endregion
}
