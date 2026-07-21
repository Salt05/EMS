using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using EMS.Core.Entities;
using EMS.Core.Entities.Enums;

namespace EMS.Mvc.TagHelpers;

[HtmlTargetElement("event-status-badge")]
public class EventStatusBadgeTagHelper : TagHelper
{
    [HtmlAttributeName("event")]
    public Event Event { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Event == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";

        string badgeClass = "";
        string badgeText = "";

        if (Event.Status == EventStatus.Cancelled)
        {
            badgeClass = "status-badge-cancelled";
            badgeText = "Đã hủy";
        }
        else if (Event.Status == EventStatus.Rejected)
        {
            badgeClass = "status-badge-rejected";
            badgeText = "Đã từ chối";
        }
        else if (Event.Status == EventStatus.Pending)
        {
            badgeClass = "status-badge-pending";
            badgeText = "Chờ phê duyệt";
        }
        else
        {
            // Trạng thái Approved (hoặc các trạng thái khác), tính theo thời gian thực tế
            var now = DateTime.UtcNow;
            
            var startTime = Event.StartTime.Kind == DateTimeKind.Utc ? Event.StartTime : Event.StartTime.ToUniversalTime();
            var endTime = Event.EndTime.Kind == DateTimeKind.Utc ? Event.EndTime : Event.EndTime.ToUniversalTime();

            if (now < startTime)
            {
                badgeClass = "status-badge-upcoming";
                badgeText = "Chưa diễn ra";
            }
            else if (now >= startTime && now <= endTime)
            {
                badgeClass = "status-badge-ongoing";
                badgeText = "Đang diễn ra";
            }
            else
            {
                badgeClass = "status-badge-ended";
                badgeText = "Đã kết thúc";
            }
        }

        // Lấy class hiện có nếu có để thực hiện merge class thay vì ghi đè hoàn toàn
        var existingClass = "";
        if (output.Attributes.ContainsName("class"))
        {
            existingClass = output.Attributes["class"].Value?.ToString() ?? "";
        }

        var combinedClass = string.IsNullOrEmpty(existingClass) 
            ? $"event-badge {badgeClass}" 
            : $"{existingClass} {badgeClass}";

        output.Attributes.RemoveAll("class");
        output.Attributes.SetAttribute("class", combinedClass.Trim());

        output.Content.SetContent(badgeText);
    }
}
