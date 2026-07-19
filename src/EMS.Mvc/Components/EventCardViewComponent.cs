using Microsoft.AspNetCore.Mvc;
using EMS.Core.Entities;
using System;

namespace EMS.Mvc.Components
{
    public class EventCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(Event eventItem)
        {
            // Compare using UTC since Firestore service converts values to Universal Time
            var now = DateTime.UtcNow;
            
            var startTime = eventItem.StartTime.Kind == DateTimeKind.Utc ? eventItem.StartTime : eventItem.StartTime.ToUniversalTime();
            var endTime = eventItem.EndTime.Kind == DateTimeKind.Utc ? eventItem.EndTime : eventItem.EndTime.ToUniversalTime();
            
            string badgeClass;
            string badgeText;

            if (now < startTime)
            {
                badgeClass = "bg-primary text-white";
                badgeText = "Sắp diễn ra";
            }
            else if (now >= startTime && now <= endTime)
            {
                badgeClass = "bg-success text-white animate-pulse"; // adding subtle pulse to ongoing events
                badgeText = "Đang diễn ra";
            }
            else
            {
                badgeClass = "bg-secondary text-white";
                badgeText = "Đã kết thúc";
            }

            ViewBag.BadgeClass = badgeClass;
            ViewBag.BadgeText = badgeText;

            return View(eventItem);
        }
    }
}
