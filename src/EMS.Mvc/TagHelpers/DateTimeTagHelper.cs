using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace EMS.Mvc.TagHelpers;

[HtmlTargetElement("datetime-format")]
public class DateTimeTagHelper : TagHelper
{
    public DateTime Date { get; set; }
    public string Format { get; set; } = "dd/MM/yyyy HH:mm";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span"; // Replaces <datetime-format> with <span>
        
        // Convert to local time if it's UTC, or format directly
        var displayDate = Date.Kind == DateTimeKind.Utc ? Date.ToLocalTime() : Date;
        
        output.Content.SetContent(displayDate.ToString(Format));
        
        // Optional: add a tooltip with the full ISO format for accessibility
        output.Attributes.SetAttribute("title", Date.ToString("O"));
    }
}
