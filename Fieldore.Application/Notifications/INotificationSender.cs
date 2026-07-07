namespace Fieldore.Application.Notifications;

/// <summary>Low-level email transport. Implementations: SMTP, SendGrid, etc.</summary>
public interface INotificationSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
