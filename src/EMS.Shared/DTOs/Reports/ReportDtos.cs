using System;
using System.Collections.Generic;

namespace EMS.Shared.DTOs.Reports;

/// <summary>Supported export file formats.</summary>
public enum ExportFormat
{
    Excel = 1,
    Pdf = 2
}

/// <summary>A rendered export file ready to be streamed back to the client.</summary>
public class ExportFileResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = "export";
}

/// <summary>One attendee/registration row in an event registration report.</summary>
public class RegistrationReportRow
{
    public int Index { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MSSV { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public bool CheckedIn { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

/// <summary>Header metadata + rows for an event registration report.</summary>
public class RegistrationReport
{
    public string EventTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Capacity { get; set; }
    public List<RegistrationReportRow> Rows { get; set; } = new();

    public int TotalRegistrations => Rows.Count;
    public int TotalCheckedIn { get { int n = 0; foreach (var r in Rows) if (r.CheckedIn) n++; return n; } }
}

/// <summary>One event row in a tenant-wide summary report.</summary>
public class EventSummaryRow
{
    public int Index { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int RegistrationCount { get; set; }
    public int CheckInCount { get; set; }
    public double CheckInRate => RegistrationCount > 0 ? Math.Round(CheckInCount * 100.0 / RegistrationCount, 1) : 0;
}

/// <summary>Header metadata + rows for a tenant summary report.</summary>
public class EventSummaryReport
{
    public string TenantName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<EventSummaryRow> Rows { get; set; } = new();
}
