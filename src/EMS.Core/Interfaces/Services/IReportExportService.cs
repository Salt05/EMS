using EMS.Shared.DTOs.Reports;

namespace EMS.Core.Interfaces.Services;

/// <summary>
/// Renders reports to downloadable Excel (.xlsx) or PDF files.
/// The service only formats data; callers are responsible for gathering and
/// authorising the underlying records.
/// </summary>
public interface IReportExportService
{
    /// <summary>Renders an event's registration/attendee list.</summary>
    ExportFileResult ExportRegistrations(RegistrationReport report, ExportFormat format);

    /// <summary>Renders a tenant-wide event summary report.</summary>
    ExportFileResult ExportEventSummary(EventSummaryReport report, ExportFormat format);
}
