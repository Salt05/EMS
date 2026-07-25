using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Infrastructure.Services;

public class DevInMemoryRewardService : IRewardCategoryService, IEventRewardService, IUserRewardService
{
    private static readonly ConcurrentBag<RewardCategory> _categories = new();
    private static readonly ConcurrentBag<EventReward> _eventRewards = new();
    private static readonly ConcurrentBag<UserRewardRecord> _userRecords = new();

    static DevInMemoryRewardService()
    {
        // Seed initial categories for dev
        var cat1 = new RewardCategory
        {
            Id = "cat-drl-1",
            TenantId = "default",
            Name = "Điểm rèn luyện",
            Type = RewardType.TrainingPoint,
            Description = "Cộng điểm rèn luyện sinh viên",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var cat2 = new RewardCategory
        {
            Id = "cat-gift-2",
            TenantId = "default",
            Name = "Quà tặng vật lý",
            Type = RewardType.PhysicalGift,
            Description = "Phần quà lưu niệm sự kiện",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var cat3 = new RewardCategory
        {
            Id = "cat-cert-3",
            TenantId = "default",
            Name = "Giấy chứng nhận",
            Type = RewardType.Certificate,
            Description = "Chứng nhận tham gia sự kiện",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _categories.Add(cat1);
        _categories.Add(cat2);
        _categories.Add(cat3);

        // Seed initial event rewards for dev
        _eventRewards.Add(new EventReward
        {
            Id = "rw-seed-1",
            EventId = "evt-workshop-ai",
            TenantId = "default",
            RewardCategoryId = "cat-drl-1",
            DetailName = "+5 Điểm rèn luyện",
            ValueOrQuantity = 5,
            Description = "Cộng điểm rèn luyện khi tham gia và điểm danh thành công",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        _eventRewards.Add(new EventReward
        {
            Id = "rw-seed-2",
            EventId = "evt-workshop-ai",
            TenantId = "default",
            RewardCategoryId = "cat-cert-3",
            DetailName = "Giấy chứng nhận tham gia Workshop AI",
            ValueOrQuantity = 1,
            Description = "Giấy chứng nhận bản điện tử gửi qua Email",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        _eventRewards.Add(new EventReward
        {
            Id = "rw-seed-3",
            EventId = "evt-music-night",
            TenantId = "default",
            RewardCategoryId = "cat-gift-2",
            DetailName = "Áo thun kỷ niệm đêm nhạc",
            ValueOrQuantity = 1,
            Description = "Dành cho 50 sinh viên check-in sớm nhất",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
    }

    #region IRewardCategoryService
    public Task<List<RewardCategory>> GetCategoriesByTenantAsync(string tenantId, bool activeOnly = true)
    {
        var result = _categories.Where(c => (string.IsNullOrEmpty(tenantId) || tenantId == "all" || c.TenantId == tenantId) && (!activeOnly || c.IsActive)).ToList();
        return Task.FromResult(result);
    }

    public Task<RewardCategory?> GetCategoryByIdAsync(string id, string tenantId)
    {
        var cat = _categories.FirstOrDefault(c => c.Id == id && (string.IsNullOrEmpty(tenantId) || tenantId == "all" || c.TenantId == tenantId));
        return Task.FromResult(cat);
    }

    public Task<RewardCategory> CreateCategoryAsync(RewardCategory category)
    {
        if (string.IsNullOrEmpty(category.Id)) category.Id = Guid.NewGuid().ToString();
        category.CreatedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        _categories.Add(category);
        return Task.FromResult(category);
    }

    public Task<bool> UpdateCategoryAsync(RewardCategory category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id && c.TenantId == category.TenantId);
        if (existing == null) return Task.FromResult(false);

        existing.Name = category.Name;
        existing.Type = category.Type;
        existing.Description = category.Description;
        existing.IsActive = category.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<bool> ToggleActiveStatusAsync(string id, string tenantId, bool isActive)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == id && (string.IsNullOrEmpty(tenantId) || tenantId == "all" || c.TenantId == tenantId));
        if (existing == null) return Task.FromResult(false);

        existing.IsActive = isActive;
        existing.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(true);
    }
    #endregion

    #region IEventRewardService
    public Task<List<EventReward>> GetRewardsByEventAsync(string eventId, string tenantId)
    {
        var list = _eventRewards.Where(r => r.EventId == eventId).ToList();
        return Task.FromResult(list);
    }

    public Task<bool> SaveEventRewardsAsync(string eventId, string tenantId, List<EventReward> rewards)
    {
        var toKeep = _eventRewards.Where(r => r.EventId != eventId).ToList();
        while (_eventRewards.TryTake(out _)) { }

        foreach (var k in toKeep) _eventRewards.Add(k);

        foreach (var r in rewards)
        {
            if (string.IsNullOrEmpty(r.Id)) r.Id = Guid.NewGuid().ToString();
            r.EventId = eventId;
            r.TenantId = tenantId;
            r.CreatedAt = DateTime.UtcNow;
            r.UpdatedAt = DateTime.UtcNow;
            _eventRewards.Add(r);
        }

        return Task.FromResult(true);
    }

    public Task<bool> DeleteRewardAsync(string rewardId, string tenantId)
    {
        var toKeep = _eventRewards.Where(r => r.Id != rewardId).ToList();
        while (_eventRewards.TryTake(out _)) { }
        foreach (var k in toKeep) _eventRewards.Add(k);
        return Task.FromResult(true);
    }
    #endregion

    #region IUserRewardService
    public Task<bool> GrantRewardsOnCheckInAsync(string tenantId, string eventId, string userId, string studentEmail, string studentName)
    {
        bool alreadyGranted = _userRecords.Any(r => r.EventId == eventId && r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));
        if (alreadyGranted)
        {
            return Task.FromResult(true);
        }

        var eventRewards = _eventRewards.Where(r => r.EventId == eventId).ToList();
        if (!eventRewards.Any())
        {
            return Task.FromResult(true);
        }

        foreach (var er in eventRewards)
        {
            var cat = _categories.FirstOrDefault(c => c.Id == er.RewardCategoryId);
            var rec = new UserRewardRecord
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                UserId = userId,
                StudentEmail = studentEmail,
                StudentName = studentName,
                EventId = eventId,
                EventTitle = "Sự kiện #" + eventId,
                RewardCategoryId = er.RewardCategoryId,
                RewardCategoryName = cat?.Name ?? "Phần thưởng",
                RewardType = cat?.Type ?? RewardType.TrainingPoint,
                DetailName = er.DetailName,
                Amount = er.ValueOrQuantity,
                GrantedAt = DateTime.UtcNow
            };
            _userRecords.Add(rec);
        }

        return Task.FromResult(true);
    }

    public Task<List<UserRewardRecord>> GetUserRewardsAsync(string studentEmail, string tenantId, RewardType? type = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _userRecords.Where(r => r.StudentEmail.Equals(studentEmail, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(tenantId) && tenantId != "all")
        {
            query = query.Where(r => r.TenantId == tenantId);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.RewardType == type.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.GrantedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.GrantedAt <= endOfDay);
        }

        var list = query.OrderByDescending(r => r.GrantedAt).ToList();
        return Task.FromResult(list);
    }

    public Task<List<StudentRewardSummaryDto>> GetTenantUserRewardStatsAsync(
        string tenantId,
        string? rewardCategoryId = null,
        RewardType? rewardType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortOrder = "desc",
        string? searchKeyword = null)
    {
        var query = _userRecords.AsQueryable();

        if (!string.IsNullOrEmpty(tenantId) && tenantId != "all")
        {
            query = query.Where(r => r.TenantId == tenantId);
        }

        if (!string.IsNullOrEmpty(rewardCategoryId))
        {
            query = query.Where(r => r.RewardCategoryId == rewardCategoryId);
        }

        if (rewardType.HasValue)
        {
            query = query.Where(r => r.RewardType == rewardType.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.GrantedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(r => r.GrantedAt <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.Trim().ToLower();
            query = query.Where(r => r.StudentName.ToLower().Contains(kw) || r.StudentEmail.ToLower().Contains(kw));
        }

        var grouped = query.GroupBy(r => r.StudentEmail)
            .Select(g => new StudentRewardSummaryDto
            {
                StudentEmail = g.Key,
                StudentName = g.First().StudentName,
                TotalAmount = g.Sum(x => x.Amount),
                RecordCount = g.Count(),
                Records = g.OrderByDescending(x => x.GrantedAt).ToList()
            });

        if (sortOrder?.ToLower() == "asc")
        {
            grouped = grouped.OrderBy(s => s.TotalAmount);
        }
        else
        {
            grouped = grouped.OrderByDescending(s => s.TotalAmount);
        }

        return Task.FromResult(grouped.ToList());
    }
    #endregion
}
