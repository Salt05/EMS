using EMS.Core.Entities;
using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Rewards;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.WebAPI.Controllers;

[ApiController]
[Route("api/rewards")]
public class RewardsController : ControllerBase
{
    private readonly IRewardCategoryService _categoryService;
    private readonly IEventRewardService _eventRewardService;
    private readonly IUserRewardService _userRewardService;

    public RewardsController(
        IRewardCategoryService categoryService,
        IEventRewardService eventRewardService,
        IUserRewardService userRewardService)
    {
        _categoryService = categoryService;
        _eventRewardService = eventRewardService;
        _userRewardService = userRewardService;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string tenantId = "default", [FromQuery] bool activeOnly = true)
    {
        var list = await _categoryService.GetCategoriesByTenantAsync(tenantId, activeOnly);
        var dtos = list.Select(MapCategoryToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(string id, [FromQuery] string tenantId = "default")
    {
        var cat = await _categoryService.GetCategoryByIdAsync(id, tenantId);
        if (cat == null) return NotFound();
        return Ok(MapCategoryToDto(cat));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] RewardCategoryDto category)
    {
        var cat = new RewardCategory
        {
            Id = category.Id,
            TenantId = string.IsNullOrEmpty(category.TenantId) ? "default" : category.TenantId,
            Name = category.Name,
            Type = (RewardType)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        var created = await _categoryService.CreateCategoryAsync(cat);
        return Ok(MapCategoryToDto(created));
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] RewardCategoryDto category)
    {
        var cat = new RewardCategory
        {
            Id = id,
            TenantId = string.IsNullOrEmpty(category.TenantId) ? "default" : category.TenantId,
            Name = category.Name,
            Type = (RewardType)(int)category.Type,
            Description = category.Description,
            IsActive = category.IsActive
        };
        var success = await _categoryService.UpdateCategoryAsync(cat);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPatch("categories/{id}/toggle")]
    public async Task<IActionResult> ToggleActiveStatus(string id, [FromQuery] bool isActive, [FromQuery] string tenantId = "default")
    {
        var success = await _categoryService.ToggleActiveStatusAsync(id, tenantId, isActive);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpGet("events/{eventId}")]
    public async Task<IActionResult> GetRewardsByEvent(string eventId, [FromQuery] string tenantId = "default")
    {
        var list = await _eventRewardService.GetRewardsByEventAsync(eventId, tenantId);
        var dtos = list.Select(r => new EventRewardDto
        {
            Id = r.Id,
            EventId = r.EventId,
            TenantId = r.TenantId,
            RewardCategoryId = r.RewardCategoryId,
            DetailName = r.DetailName,
            ValueOrQuantity = r.ValueOrQuantity,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
        return Ok(dtos);
    }

    [HttpPost("events/{eventId}")]
    public async Task<IActionResult> SaveEventRewards(string eventId, [FromQuery] string tenantId = "default", [FromBody] List<EventRewardDto>? rewards = null)
    {
        rewards ??= new List<EventRewardDto>();
        var entities = rewards.Select(r => new EventReward
        {
            Id = r.Id,
            EventId = eventId,
            TenantId = tenantId,
            RewardCategoryId = r.RewardCategoryId,
            DetailName = r.DetailName,
            ValueOrQuantity = r.ValueOrQuantity,
            Description = r.Description
        }).ToList();

        var success = await _eventRewardService.SaveEventRewardsAsync(eventId, tenantId, entities);
        return Ok(success);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetTenantUserRewardStats(
        [FromQuery] string tenantId = "default",
        [FromQuery] string? rewardCategoryId = null,
        [FromQuery] RewardTypeDto? rewardType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? searchKeyword = null)
    {
        RewardType? coreRewardType = rewardType.HasValue ? (RewardType)(int)rewardType.Value : null;
        var coreStats = await _userRewardService.GetTenantUserRewardStatsAsync(tenantId, rewardCategoryId, coreRewardType, fromDate, toDate, sortOrder, searchKeyword);

        var dtos = coreStats.Select(s => new EMS.Shared.DTOs.Rewards.StudentRewardSummaryDto
        {
            StudentEmail = s.StudentEmail,
            StudentName = s.StudentName,
            TotalAmount = s.TotalAmount,
            RecordCount = s.RecordCount,
            Records = s.Records.Select(r => new UserRewardRecordDto
            {
                Id = r.Id,
                TenantId = r.TenantId,
                UserId = r.UserId,
                StudentEmail = r.StudentEmail,
                StudentName = r.StudentName,
                EventId = r.EventId,
                EventTitle = r.EventTitle,
                RewardCategoryId = r.RewardCategoryId,
                RewardCategoryName = r.RewardCategoryName,
                RewardType = (RewardTypeDto)(int)r.RewardType,
                DetailName = r.DetailName,
                Amount = r.Amount,
                GrantedAt = r.GrantedAt
            }).ToList()
        }).ToList();

        return Ok(dtos);
    }

    private static RewardCategoryDto MapCategoryToDto(RewardCategory cat)
    {
        return new RewardCategoryDto
        {
            Id = cat.Id,
            TenantId = cat.TenantId,
            Name = cat.Name,
            Type = (RewardTypeDto)(int)cat.Type,
            Description = cat.Description,
            IsActive = cat.IsActive
        };
    }
}
