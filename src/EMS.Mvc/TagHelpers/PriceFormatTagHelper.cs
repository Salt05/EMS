using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Globalization;

namespace EMS.Mvc.TagHelpers;

[HtmlTargetElement("price-format")]
public class PriceFormatTagHelper : TagHelper
{
    [HtmlAttributeName("price")]
    public decimal Price { get; set; }

    [HtmlAttributeName("is-free")]
    public bool IsFree { get; set; }

    [HtmlAttributeName("currency")]
    public string Currency { get; set; } = "VNĐ";

    [HtmlAttributeName("free-text")]
    public string FreeText { get; set; } = "MIỄN PHÍ";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";

        if (IsFree || Price <= 0)
        {
            if (!HasColorStyle(output))
            {
                AppendStyle(output, "color: var(--success);");
            }
            output.Content.SetContent(FreeText);
        }
        else
        {
            if (!HasColorStyle(output))
            {
                AppendStyle(output, "color: var(--danger);");
            }
            var formattedPrice = Price.ToString("N0", new CultureInfo("vi-VN"));
            output.Content.SetContent($"{formattedPrice} {Currency}");
        }
    }

    private bool HasColorStyle(TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("style"))
        {
            var style = output.Attributes["style"].Value?.ToString() ?? "";
            return style.Contains("color:") || style.Contains("color :");
        }
        return false;
    }

    private void AppendStyle(TagHelperOutput output, string style)
    {
        var existingStyle = "";
        if (output.Attributes.ContainsName("style"))
        {
            existingStyle = output.Attributes["style"].Value?.ToString() ?? "";
            if (!existingStyle.EndsWith(";"))
            {
                existingStyle += ";";
            }
        }
        var combinedStyle = string.IsNullOrEmpty(existingStyle) ? style : $"{existingStyle} {style}";
        output.Attributes.RemoveAll("style");
        output.Attributes.SetAttribute("style", combinedStyle.Trim());
    }
}
