using ClosedXML.Excel;
using EMS.Core.Interfaces.Services;
using EMS.Shared.DTOs.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EMS.Infrastructure.Services;

/// <summary>
/// Renders reports to Excel (ClosedXML) and PDF (QuestPDF) byte streams.
/// </summary>
public class ReportExportService : IReportExportService
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    static ReportExportService()
    {
        // QuestPDF Community licence — free for individuals and small businesses.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ===================================================================
    // Registration / attendee report
    // ===================================================================

    public ExportFileResult ExportRegistrations(RegistrationReport report, ExportFormat format)
    {
        var slug = Slug(report.EventTitle);
        return format == ExportFormat.Pdf
            ? new ExportFileResult
            {
                Content = BuildRegistrationsPdf(report),
                ContentType = PdfContentType,
                FileName = $"danh-sach-dang-ky-{slug}.pdf"
            }
            : new ExportFileResult
            {
                Content = BuildRegistrationsExcel(report),
                ContentType = ExcelContentType,
                FileName = $"danh-sach-dang-ky-{slug}.xlsx"
            };
    }

    private static byte[] BuildRegistrationsExcel(RegistrationReport report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Đăng ký");

        // Title block
        ws.Cell(1, 1).Value = "DANH SÁCH ĐĂNG KÝ SỰ KIỆN";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Sự kiện: {report.EventTitle}";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(3, 1).Value = $"Địa điểm: {report.Location}";
        ws.Range(3, 1, 3, 7).Merge();
        ws.Cell(4, 1).Value =
            $"Thời gian: {report.StartTime.ToLocalTime():dd/MM/yyyy HH:mm} - {report.EndTime.ToLocalTime():dd/MM/yyyy HH:mm}";
        ws.Range(4, 1, 4, 7).Merge();
        ws.Cell(5, 1).Value =
            $"Tổng đăng ký: {report.TotalRegistrations}   |   Đã điểm danh: {report.TotalCheckedIn}   |   Sức chứa: {report.Capacity}";
        ws.Range(5, 1, 5, 7).Merge();

        // Header row
        const int headerRow = 7;
        string[] headers = { "STT", "Họ tên", "MSSV", "Email", "Trạng thái", "Điểm danh", "Thời gian đăng ký" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = headerRow + 1;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.Index;
            ws.Cell(row, 2).Value = r.FullName;
            ws.Cell(row, 3).Value = r.MSSV;
            ws.Cell(row, 4).Value = r.Email;
            ws.Cell(row, 5).Value = r.StatusName;
            ws.Cell(row, 6).Value = r.CheckedIn ? "✓" : "";
            ws.Cell(row, 7).Value = r.RegisteredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            row++;
        }

        var dataRange = ws.Range(headerRow, 1, Math.Max(headerRow, row - 1), 7);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();

        return ToBytes(workbook);
    }

    private static byte[] BuildRegistrationsPdf(RegistrationReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("DANH SÁCH ĐĂNG KÝ SỰ KIỆN")
                        .FontSize(16).Bold().FontColor(Colors.Grey.Darken4);
                    col.Item().PaddingTop(4).Text(report.EventTitle).FontSize(11).SemiBold();
                    col.Item().Text($"Địa điểm: {report.Location}").FontColor(Colors.Grey.Darken1);
                    col.Item().Text(
                        $"Thời gian: {report.StartTime.ToLocalTime():dd/MM/yyyy HH:mm} - {report.EndTime.ToLocalTime():dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                    col.Item().Text(
                        $"Tổng đăng ký: {report.TotalRegistrations}  |  Đã điểm danh: {report.TotalCheckedIn}  |  Sức chứa: {report.Capacity}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(28);   // STT
                        cols.RelativeColumn(3);     // Họ tên
                        cols.RelativeColumn(2);     // MSSV
                        cols.RelativeColumn(3);     // Email
                        cols.RelativeColumn(2);     // Trạng thái
                        cols.ConstantColumn(55);    // Điểm danh
                        cols.RelativeColumn(2);     // Thời gian
                    });

                    table.Header(header =>
                    {
                        void H(string text) => header.Cell().Background(Colors.Grey.Darken4)
                            .Padding(4).Text(text).FontColor(Colors.White).SemiBold();
                        H("STT"); H("Họ tên"); H("MSSV"); H("Email");
                        H("Trạng thái"); H("Điểm danh"); H("Đăng ký");
                    });

                    foreach (var r in report.Rows)
                    {
                        void C(string text) => table.Cell().BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
                        C(r.Index.ToString());
                        C(r.FullName);
                        C(r.MSSV);
                        C(r.Email);
                        C(r.StatusName);
                        C(r.CheckedIn ? "✓" : "-");
                        C(r.RegisteredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span($"Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm} — Trang ");
                    text.CurrentPageNumber();
                    text.Span("/");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // ===================================================================
    // Tenant event summary report
    // ===================================================================

    public ExportFileResult ExportEventSummary(EventSummaryReport report, ExportFormat format)
    {
        return format == ExportFormat.Pdf
            ? new ExportFileResult
            {
                Content = BuildSummaryPdf(report),
                ContentType = PdfContentType,
                FileName = "bao-cao-tong-hop-su-kien.pdf"
            }
            : new ExportFileResult
            {
                Content = BuildSummaryExcel(report),
                ContentType = ExcelContentType,
                FileName = "bao-cao-tong-hop-su-kien.xlsx"
            };
    }

    private static byte[] BuildSummaryExcel(EventSummaryReport report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tổng hợp");

        ws.Cell(1, 1).Value = "BÁO CÁO TỔNG HỢP HOẠT ĐỘNG SỰ KIỆN";
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Tổ chức: {report.TenantName}";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(3, 1).Value = $"Ngày xuất: {report.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}";
        ws.Range(3, 1, 3, 7).Merge();

        const int headerRow = 5;
        string[] headers = { "STT", "Tên sự kiện", "Người tổ chức", "Trạng thái", "Thời gian", "Đăng ký", "Điểm danh", "Tỷ lệ (%)" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F2937");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = headerRow + 1;
        foreach (var r in report.Rows)
        {
            ws.Cell(row, 1).Value = r.Index;
            ws.Cell(row, 2).Value = r.EventTitle;
            ws.Cell(row, 3).Value = r.OrganizerName;
            ws.Cell(row, 4).Value = r.StatusName;
            ws.Cell(row, 5).Value = r.StartTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 6).Value = r.RegistrationCount;
            ws.Cell(row, 7).Value = r.CheckInCount;
            ws.Cell(row, 8).Value = r.CheckInRate;
            row++;
        }

        // Totals row
        if (report.Rows.Count > 0)
        {
            ws.Cell(row, 2).Value = "TỔNG CỘNG";
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 6).Value = report.Rows.Sum(r => r.RegistrationCount);
            ws.Cell(row, 7).Value = report.Rows.Sum(r => r.CheckInCount);
            ws.Row(row).Style.Font.Bold = true;
        }

        var dataRange = ws.Range(headerRow, 1, Math.Max(headerRow, row), 8);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Columns().AdjustToContents();

        return ToBytes(workbook);
    }

    private static byte[] BuildSummaryPdf(EventSummaryReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("BÁO CÁO TỔNG HỢP HOẠT ĐỘNG SỰ KIỆN")
                        .FontSize(16).Bold().FontColor(Colors.Grey.Darken4);
                    col.Item().PaddingTop(4).Text($"Tổ chức: {report.TenantName}").SemiBold();
                    col.Item().Text($"Ngày xuất: {report.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(28);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(50);
                        cols.ConstantColumn(45);
                    });

                    table.Header(header =>
                    {
                        void H(string text) => header.Cell().Background(Colors.Grey.Darken4)
                            .Padding(4).Text(text).FontColor(Colors.White).SemiBold();
                        H("STT"); H("Tên sự kiện"); H("Người tổ chức"); H("Trạng thái");
                        H("Thời gian"); H("Đăng ký"); H("Điểm danh"); H("Tỷ lệ");
                    });

                    foreach (var r in report.Rows)
                    {
                        void C(string text) => table.Cell().BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);
                        C(r.Index.ToString());
                        C(r.EventTitle);
                        C(r.OrganizerName);
                        C(r.StatusName);
                        C(r.StartTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        C(r.RegistrationCount.ToString());
                        C(r.CheckInCount.ToString());
                        C($"{r.CheckInRate}%");
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span("/");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    // ===================================================================
    // Helpers
    // ===================================================================

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "su-kien";
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "su-kien" : slug[..Math.Min(slug.Length, 40)];
    }
}
