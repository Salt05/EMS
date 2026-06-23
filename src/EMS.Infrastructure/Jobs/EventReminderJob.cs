using EMS.Core.Entities.Enums;
using EMS.Core.Interfaces.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Jobs;

/// <summary>
/// Background job (scheduled via Hangfire) that emails a reminder to confirmed attendees
/// of events starting within the next 24 hours. Runs across all tenants and marks each
/// registration as reminded so the same person is not emailed twice.
/// </summary>
public class EventReminderJob
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventReminderJob> _logger;

    private const string EventsCollection = "events";
    private const string RegistrationsCollection = "registrations";
    private const string UsersCollection = "users";
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    public EventReminderJob(
        FirestoreDb firestoreDb,
        IEmailService emailService,
        ILogger<EventReminderJob> logger)
    {
        _firestoreDb = firestoreDb;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendUpcomingEventRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var windowEnd = now.Add(ReminderWindow);

        _logger.LogInformation($"[EventReminderJob] Scanning events starting before {windowEnd:u}");

        // Range query on startTime only (no composite index needed); status filtered in memory.
        var eventsSnapshot = await _firestoreDb.Collection(EventsCollection)
            .WhereGreaterThanOrEqualTo("startTime", Timestamp.FromDateTime(now))
            .WhereLessThanOrEqualTo("startTime", Timestamp.FromDateTime(windowEnd))
            .GetSnapshotAsync();

        var sent = 0;

        foreach (var evDoc in eventsSnapshot.Documents)
        {
            var ev = evDoc.ToDictionary();

            if (!(ev.TryGetValue("status", out var st) && st is long stLong && (int)stLong == (int)EventStatus.Approved))
                continue;

            var eventId = ev.TryGetValue("id", out var id) ? id?.ToString() ?? evDoc.Id : evDoc.Id;
            var tenantId = ev.TryGetValue("tenantId", out var tid) ? tid?.ToString() : null;
            var title = ev.TryGetValue("title", out var t) ? t?.ToString() ?? "(event)" : "(event)";
            var startTime = ev.TryGetValue("startTime", out var s) && s is Timestamp sTs ? sTs.ToDateTime() : now;

            sent += await RemindEventAttendeesAsync(eventId, title, startTime, tenantId);
        }

        _logger.LogInformation($"[EventReminderJob] Completed. Reminders sent: {sent}");
    }

    private async Task<int> RemindEventAttendeesAsync(string eventId, string title, DateTime startTime, string? tenantId)
    {
        // 1. Fetch Tenant branding info dynamically
        string? fromAddress = null;
        string? fromDisplayName = null;
        if (!string.IsNullOrEmpty(tenantId))
        {
            try
            {
                var tenantDoc = await _firestoreDb.Collection("tenants").Document(tenantId).GetSnapshotAsync();
                if (tenantDoc.Exists)
                {
                    var tenantData = tenantDoc.ToDictionary();
                    fromAddress = tenantData.TryGetValue("email", out var temail) ? temail?.ToString() : null;
                    fromDisplayName = tenantData.TryGetValue("name", out var tname) ? tname?.ToString() : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to look up Tenant info for ID {tenantId}");
            }
        }

        var regsSnapshot = await _firestoreDb.Collection(RegistrationsCollection)
            .WhereEqualTo("eventId", eventId)
            .WhereEqualTo("status", (int)RegistrationStatus.Confirmed)
            .GetSnapshotAsync();

        var sent = 0;

        foreach (var regDoc in regsSnapshot.Documents)
        {
            var reg = regDoc.ToDictionary();

            // Skip anyone already reminded for this event.
            if (reg.TryGetValue("reminderSent", out var rs) && rs is bool rsBool && rsBool)
                continue;

            var userId = reg.TryGetValue("userId", out var uid) ? uid?.ToString() : null;
            if (string.IsNullOrEmpty(userId)) continue;

            var email = await GetUserEmailAsync(userId);
            if (string.IsNullOrEmpty(email)) continue;

            var subject = $"Reminder: {title} is starting soon";
            var body = $"<p>Hi,</p><p>This is a reminder that <strong>{title}</strong> starts at " +
                       $"<strong>{startTime:f} (UTC)</strong>.</p><p>See you there!</p>";

            // Only update reminderSent flag in database if the email was successfully sent.
            var success = await _emailService.SendEmailAsync(email, subject, body, fromAddress, fromDisplayName);
            if (success)
            {
                await regDoc.Reference.SetAsync(new Dictionary<string, object>
                {
                    { "reminderSent", true },
                    { "reminderSentAt", DateTime.UtcNow },
                    { "updatedAt", DateTime.UtcNow }
                }, SetOptions.MergeAll);

                sent++;
            }
            else
            {
                _logger.LogWarning($"Skipped updating reminderSent flag for registration {regDoc.Id} because email delivery failed.");
            }
        }

        return sent;
    }

    private async Task<string?> GetUserEmailAsync(string userId)
    {
        try
        {
            var userDoc = await _firestoreDb.Collection(UsersCollection).Document(userId).GetSnapshotAsync();
            if (!userDoc.Exists) return null;

            var user = userDoc.ToDictionary();
            return user.TryGetValue("email", out var email) ? email?.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[EventReminderJob] Failed to look up email for user {userId}");
            return null;
        }
    }
}
