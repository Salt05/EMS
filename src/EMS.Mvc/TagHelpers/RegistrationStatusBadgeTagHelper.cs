using Microsoft.AspNetCore.Razor.TagHelpers;
using EMS.Core.Entities.Enums;

namespace EMS.Mvc.TagHelpers;

[HtmlTargetElement("registration-status-badge")]
public class RegistrationStatusBadgeTagHelper : TagHelper
{
    [HtmlAttributeName("status")]
    public RegistrationStatus Status { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";

        string badgeClass;
        string badgeText;
        string iconClass;

        switch (Status)
        {
            case RegistrationStatus.Approved: // alias for Confirmed
                badgeClass = "status-approved";
                iconClass = "ri-checkbox-circle-line";
                badgeText = "Đã xác nhận";
                break;
            case RegistrationStatus.Pending:
                badgeClass = "status-pending";
                iconClass = "ri-time-line";
                badgeText = "Chờ phê duyệt";
                break;
            case RegistrationStatus.PendingPayment:
                badgeClass = "status-pending";
                iconClass = "ri-wallet-2-line";
                badgeText = "Chờ thanh toán";
                break;
            case RegistrationStatus.Rejected:
                badgeClass = "status-rejected";
                iconClass = "ri-close-circle-line";
                badgeText = "Bị từ chối";
                break;
            case RegistrationStatus.Cancelled:
                badgeClass = "status-cancelled";
                iconClass = "ri-close-circle-line";
                badgeText = "Đã hủy";
                break;
            case RegistrationStatus.Waitlisted:
                badgeClass = "status-pending";
                iconClass = "ri-user-shared-line";
                badgeText = "Danh sách chờ";
                break;
            default:
                badgeClass = "bg-light text-dark";
                iconClass = "ri-question-line";
                badgeText = "Không rõ";
                break;
        }

        var existingClass = "";
        if (output.Attributes.ContainsName("class"))
        {
            existingClass = output.Attributes["class"].Value?.ToString() ?? "";
        }

        var combinedClass = string.IsNullOrEmpty(existingClass)
            ? $"status-badge {badgeClass}"
            : $"{existingClass} {badgeClass}";

        output.Attributes.RemoveAll("class");
        output.Attributes.SetAttribute("class", combinedClass.Trim());

        output.Content.SetHtmlContent($"<i class=\"{iconClass}\"></i> {badgeText}");
    }
}
