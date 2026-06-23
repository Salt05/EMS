using System.Net;
using System.Net.Mail;
using EMS.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Services;

/// <summary>
/// Sends email over SMTP using the "Email" configuration section. When the SMTP host is not
/// configured the service logs the message instead of throwing, so background jobs stay healthy
/// in environments without mail credentials (dev / CI).
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? fromAddress = null, string? fromDisplayName = null)
    {
        var host = _configuration["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation($"[Email skipped — SMTP not configured] To: {to} | Subject: {subject}");
            return false;
        }

        try
        {
            var port = int.TryParse(_configuration["Email:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            
            var fromEmail = !string.IsNullOrEmpty(fromAddress) 
                ? fromAddress 
                : (_configuration["Email:From"] ?? username ?? "no-reply@ems.local");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrEmpty(username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(username, password)
            };

            using var message = !string.IsNullOrEmpty(fromDisplayName)
                ? new MailMessage(new MailAddress(fromEmail, fromDisplayName), new MailAddress(to)) { Subject = subject, Body = htmlBody, IsBodyHtml = true }
                : new MailMessage(fromEmail, to, subject, htmlBody) { IsBodyHtml = true };

            await client.SendMailAsync(message);
            _logger.LogInformation($"Email sent to {to}: {subject}");
            return true;
        }
        catch (Exception ex)
        {
            // Never bubble up: a failed reminder email shouldn't crash the background job.
            _logger.LogError(ex, $"Failed to send email to {to}");
            return false;
        }
    }
}
