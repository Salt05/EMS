using EMS.Core.Entities.Enums;
using System;
using System.Collections.Generic;

namespace EMS.Core.Entities;

public class Registration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TenantId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Approval / rejection tracking
    public string? ProcessedById { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Cancellation tracking
    public DateTime? CancelledAt { get; set; }

    // Check-in tracking
    public string? CheckInCode { get; set; }
    public DateTime? CheckInCodeExpiresAt { get; set; }
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }

    // Reminder tracking (set by the background reminder job)
    public bool ReminderSent { get; set; }
    public DateTime? ReminderSentAt { get; set; }

    // Incoming branch fields (for Student Portal compatibility)
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;

    // Payment tracking (VNPAY integration)
    public bool IsPaid { get; set; }
    public DateTime? PaymentExpiresAt { get; set; }
    public string? PaymentTransactionId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OrganizerRevenue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object> ToFirestoreDocument()
    {
        var doc = new Dictionary<string, object>
        {
            { "id", Id },
            { "tenantId", TenantId },
            { "eventId", EventId },
            { "userId", UserId },
            { "note", Note ?? "" },
            { "status", (int)Status },
            { "registeredAt", RegisteredAt.ToUniversalTime() },
            { "processedById", ProcessedById ?? "" },
            { "rejectionReason", RejectionReason ?? "" },
            { "checkInCode", CheckInCode ?? "" },
            { "checkedIn", CheckedIn },
            { "reminderSent", ReminderSent },
            { "studentEmail", StudentEmail },
            { "studentName", StudentName },
            { "isPaid", IsPaid },
            { "platformFee", (double)PlatformFee },
            { "organizerRevenue", (double)OrganizerRevenue },
            { "createdAt", CreatedAt.ToUniversalTime() },
            { "updatedAt", UpdatedAt.ToUniversalTime() }
        };

        // Omit nullable timestamps when unset to avoid pushing DateTime.MinValue to Firestore.
        if (ProcessedAt.HasValue) doc["processedAt"] = ProcessedAt.Value.ToUniversalTime();
        if (CancelledAt.HasValue) doc["cancelledAt"] = CancelledAt.Value.ToUniversalTime();
        if (CheckInCodeExpiresAt.HasValue) doc["checkInCodeExpiresAt"] = CheckInCodeExpiresAt.Value.ToUniversalTime();
        if (CheckedInAt.HasValue) doc["checkedInAt"] = CheckedInAt.Value.ToUniversalTime();
        if (ReminderSentAt.HasValue) doc["reminderSentAt"] = ReminderSentAt.Value.ToUniversalTime();
        if (PaymentTransactionId != null) doc["paymentTransactionId"] = PaymentTransactionId;
        if (PaymentDate.HasValue) doc["paymentDate"] = PaymentDate.Value.ToUniversalTime();
        if (PaymentExpiresAt.HasValue) doc["paymentExpiresAt"] = PaymentExpiresAt.Value.ToUniversalTime();

        return doc;
    }
}
