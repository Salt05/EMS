namespace EMS.Core.Interfaces.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email. Implementations should be resilient: a missing SMTP configuration
    /// must not throw, so background jobs keep running in environments without mail set up.
    /// Returns true if the email was successfully sent; false otherwise.
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? fromAddress = null, string? fromDisplayName = null);
}
