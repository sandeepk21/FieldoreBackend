using System.Net;
using System.Net.Mail;
using Fieldore.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fieldore.Infrastructure.Notifications;

/// <summary>
/// Sends via SMTP when configured (<c>Smtp:Host</c>). When not configured it logs the email
/// instead of sending — so the app runs in dev without a mail server.
/// </summary>
public sealed class SmtpNotificationSender(IConfiguration configuration, ILogger<SmtpNotificationSender> logger)
    : INotificationSender
{
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = configuration["Smtp:Host"];
        var fromEmail = configuration["Smtp:FromEmail"] ?? "no-reply@fieldore.app";
        var fromName = configuration["Smtp:FromName"] ?? "Fieldore";

        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation("[Email suppressed — no SMTP configured] to={To} subject={Subject}", toEmail, subject);
            return;
        }

        try
        {
            var port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(configuration["Smtp:User"], configuration["Smtp:Password"]),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let email failures break the calling flow (e.g. a webhook).
            logger.LogWarning(ex, "Failed to send email to {To} ({Subject})", toEmail, subject);
        }
    }
}
