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

            return MapToCategory(snapshot);
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

            // Fetch actual Event Title
            string eventTitle = "Sự kiện #" + eventId;
            try
            {
                var eventDoc = await _firestoreDb.Collection("events").Document(eventId).GetSnapshotAsync();
                if (eventDoc.Exists && eventDoc.TryGetValue<string>("title", out var title) && !string.IsNullOrEmpty(title))
                {
                    eventTitle = title;
                }
            }
            catch { }

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
                    { "eventTitle", eventTitle },
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
            // Retroactively grant rewards for any checked-in events of this student that haven't been granted yet
            try
            {
                var allRegs = await _firestoreDb.Collection("registrations")
                    .WhereEqualTo("studentEmail", studentEmail.ToLowerInvariant())
                    .GetSnapshotAsync();

                foreach (var doc in allRegs.Documents)
                {
                    var dict = doc.ToDictionary();
                    var isCheckedIn = dict.TryGetValue("checkedIn", out var ci) && ci is bool ciBool && ciBool;
                    if (!isCheckedIn) continue;

                    var eId = dict.TryGetValue("eventId", out var eid) ? eid?.ToString() ?? "" : "";
                    var tId = dict.TryGetValue("tenantId", out var tid) ? tid?.ToString() ?? tenantId : tenantId;
                    var uId = dict.TryGetValue("userId", out var uid) ? uid?.ToString() ?? "" : "";
                    var sName = dict.TryGetValue("studentName", out var sn) ? sn?.ToString() ?? studentEmail : studentEmail;

                    if (!string.IsNullOrEmpty(eId))
                    {
                        await GrantRewardsOnCheckInAsync(tId, eId, uId, studentEmail, sName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed retroactive reward check for {studentEmail}");
            }

            Query query = _firestoreDb.Collection(UserRecordsCollection).WhereEqualTo("studentEmail", studentEmail.ToLowerInvariant());

            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            var snapshot = await query.GetSnapshotAsync();
            var list = new List<UserRewardRecord>();

            foreach (var doc in snapshot.Documents)
            {
                var rec = MapToUserRecord(doc);

                // Auto-correct rewardType if category exists and differs
                if (!string.IsNullOrEmpty(rec.RewardCategoryId))
                {
                    var cat = await GetCategoryByIdAsync(rec.RewardCategoryId, tenantId);
                    if (cat != null && rec.RewardType != cat.Type)
                    {
                        rec.RewardType = cat.Type;
                        rec.RewardCategoryName = cat.Name;

                        _ = doc.Reference.UpdateAsync(new Dictionary<string, object>
                        {
                            { "rewardType", (int)cat.Type },
                            { "rewardCategoryName", cat.Name }
                        });
                    }
                }

                list.Add(rec);
            }

            if (type.HasValue)
            {
                list = list.Where(r => r.RewardType == type.Value).ToList();
            }

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
            // Retroactively grant rewards for any checked-in registrations across all events that haven't been granted yet
            try
            {
                var allRegsSnapshot = await _firestoreDb.Collection("registrations").GetSnapshotAsync();

                foreach (var doc in allRegsSnapshot.Documents)
                {
                    var dict = doc.ToDictionary();
                    var isCheckedIn = dict.TryGetValue("checkedIn", out var ci) && ci is bool ciBool && ciBool;
                    if (!isCheckedIn) continue;

                    var eId = dict.TryGetValue("eventId", out var eid) ? eid?.ToString() ?? "" : "";
                    var tId = dict.TryGetValue("tenantId", out var tid) ? tid?.ToString() ?? tenantId : tenantId;
                    var uId = dict.TryGetValue("userId", out var uid) ? uid?.ToString() ?? "" : "";
                    var sEmail = dict.TryGetValue("studentEmail", out var se) ? se?.ToString() ?? "" : "";
                    var sName = dict.TryGetValue("studentName", out var sn) ? sn?.ToString() ?? sEmail : sEmail;

                    if (!string.IsNullOrEmpty(eId) && !string.IsNullOrEmpty(sEmail))
                    {
                        await GrantRewardsOnCheckInAsync(tId, eId, uId, sEmail, sName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed retroactive reward check in GetTenantUserRewardStatsAsync");
            }

            Query query = _firestoreDb.Collection(UserRecordsCollection);

            if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                query = query.WhereEqualTo("tenantId", tenantId);
            }

            var snapshot = await query.GetSnapshotAsync();
            var allRecords = new List<UserRewardRecord>();

            foreach (var doc in snapshot.Documents)
            {
                var rec = MapToUserRecord(doc);

                // Auto-correct rewardType if category exists and differs
                if (!string.IsNullOrEmpty(rec.RewardCategoryId))
                {
                    var cat = await GetCategoryByIdAsync(rec.RewardCategoryId, tenantId);
                    if (cat != null && rec.RewardType != cat.Type)
                    {
                        rec.RewardType = cat.Type;
                        rec.RewardCategoryName = cat.Name;

                        _ = doc.Reference.UpdateAsync(new Dictionary<string, object>
                        {
                            { "rewardType", (int)cat.Type },
                            { "rewardCategoryName", cat.Name }
                        });
                    }
                }

                allRecords.Add(rec);
            }

            if (!string.IsNullOrEmpty(rewardCategoryId))
            {
                var targetCat = await GetCategoryByIdAsync(rewardCategoryId, tenantId);
                if (targetCat != null)
                {
                    allRecords = allRecords.Where(r => r.RewardCategoryId == rewardCategoryId || r.RewardType == targetCat.Type).ToList();
                }
                else
                {
                    allRecords = allRecords.Where(r => r.RewardCategoryId == rewardCategoryId).ToList();
                }
            }

            if (rewardType.HasValue)
            {
                allRecords = allRecords.Where(r => r.RewardType == rewardType.Value).ToList();
            }

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
