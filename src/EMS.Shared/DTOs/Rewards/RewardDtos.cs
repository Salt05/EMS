using System;
using System.Collections.Generic;

namespace EMS.Shared.DTOs.Rewards;

public enum RewardTypeDto
{
    TrainingPoint = 1,   // Điểm rèn luyện
    PhysicalGift = 2,    // Quà tặng vật lý
    Voucher = 3,         // Voucher / Giftcode
    Certificate = 4,     // Giấy chứng nhận
    InternalCoin = 5     // Coin nội bộ
}

public class RewardCategoryDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RewardTypeDto Type { get; set; } = RewardTypeDto.TrainingPoint;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EventRewardDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RewardCategoryId { get; set; } = string.Empty;
    public string DetailName { get; set; } = string.Empty;
    public double ValueOrQuantity { get; set; } = 1.0;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UserRewardRecordDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string RewardCategoryId { get; set; } = string.Empty;
    public string RewardCategoryName { get; set; } = string.Empty;
    public RewardTypeDto RewardType { get; set; } = RewardTypeDto.TrainingPoint;
    public string DetailName { get; set; } = string.Empty;
    public double Amount { get; set; } = 1.0;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}

public class StudentRewardSummaryDto
{
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public double TotalAmount { get; set; }
    public int RecordCount { get; set; }
    public List<UserRewardRecordDto> Records { get; set; } = new();
}
