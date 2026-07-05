using EMS.Infrastructure.Services;
using EMS.Shared.DTOs.Reports;
using Xunit;

namespace EMS.Tests;

public class ReportExportServiceTests
{
    private static RegistrationReport SampleRegistrations() => new()
    {
        EventTitle = "Hackathon HUFLIT 2026",
        Location = "Hội trường A",
        StartTime = DateTime.UtcNow,
        EndTime = DateTime.UtcNow.AddHours(3),
        Capacity = 150,
        Rows =
        {
            new RegistrationReportRow { Index = 1, FullName = "Nguyễn Văn A", MSSV = "20001", Email = "a@huflit.edu.vn", StatusName = "Confirmed", CheckedIn = true, RegisteredAt = DateTime.UtcNow },
            new RegistrationReportRow { Index = 2, FullName = "Trần Thị B", MSSV = "20002", Email = "b@huflit.edu.vn", StatusName = "Pending", CheckedIn = false, RegisteredAt = DateTime.UtcNow }
        }
    };

    private static EventSummaryReport SampleSummary() => new()
    {
        TenantName = "ĐH HUFLIT",
        Rows =
        {
            new EventSummaryRow { Index = 1, EventTitle = "Workshop Git", OrganizerName = "BTC", StatusName = "Ended", StartTime = DateTime.UtcNow, RegistrationCount = 85, CheckInCount = 78 }
        }
    };

    [Fact]
    public void ExportRegistrations_Excel_ProducesXlsx()
    {
        var svc = new ReportExportService();
        var result = svc.ExportRegistrations(SampleRegistrations(), ExportFormat.Excel);

        Assert.NotEmpty(result.Content);
        Assert.EndsWith(".xlsx", result.FileName);
        // xlsx is a ZIP archive → starts with "PK".
        Assert.Equal((byte)'P', result.Content[0]);
        Assert.Equal((byte)'K', result.Content[1]);
    }

    [Fact]
    public void ExportRegistrations_Pdf_ProducesPdf()
    {
        var svc = new ReportExportService();
        var result = svc.ExportRegistrations(SampleRegistrations(), ExportFormat.Pdf);

        Assert.NotEmpty(result.Content);
        Assert.EndsWith(".pdf", result.FileName);
        // PDF files begin with "%PDF".
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(result.Content, 0, 4));
    }

    [Fact]
    public void ExportEventSummary_BothFormats_Produce()
    {
        var svc = new ReportExportService();
        Assert.NotEmpty(svc.ExportEventSummary(SampleSummary(), ExportFormat.Excel).Content);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(
            svc.ExportEventSummary(SampleSummary(), ExportFormat.Pdf).Content, 0, 4));
    }
}
