namespace EMS.Core.Interfaces.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends an email. Implementations should be resilient: a missing SMTP configuration
    /// must not throw, so background jobs keep running in environments without mail set up.
    /// </summary>
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
