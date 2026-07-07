using Fieldore.Application.Notifications;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fieldore.Infrastructure.Notifications;

public sealed class SubscriptionNotifier(
    FieldoreDbContext dbContext,
    INotificationSender sender,
    IConfiguration configuration,
    ILogger<SubscriptionNotifier> logger) : ISubscriptionNotifier
{
    public async Task NotifyAsync(Guid businessId, SubscriptionEmailKind kind, CancellationToken cancellationToken = default)
    {
        try
        {
            var business = await dbContext.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null || string.IsNullOrWhiteSpace(business.Email)) return;

            // Honor the owner's email preference when present (default = send).
            var profileId = await dbContext.UserProfiles.AsNoTracking()
                .Where(p => p.AuthUserId == business.AuthUserId)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (profileId is not null)
            {
                var emailEnabled = await dbContext.UserNotificationPreferences.AsNoTracking()
                    .Where(x => x.UserProfileId == profileId)
                    .Select(x => (bool?)x.EmailEnabled)
                    .FirstOrDefaultAsync(cancellationToken);
                if (emailEnabled == false) return;
            }

            var (subject, body) = Render(kind, business.Name);
            await sender.SendEmailAsync(business.Email!, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Subscription notification {Kind} failed for business {BusinessId}", kind, businessId);
        }
    }

    private (string Subject, string Body) Render(SubscriptionEmailKind kind, string businessName)
    {
        var portalUrl = $"{(configuration["Web:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/')}/portal";

        var (subject, heading, message, cta) = kind switch
        {
            SubscriptionEmailKind.TrialStarted =>
                ("Your Fieldore trial has started", "Welcome to Fieldore 🎉",
                 "Your free trial is active. Explore everything your plan offers.", "Go to your dashboard"),
            SubscriptionEmailKind.Activated =>
                ("Your Fieldore subscription is active", "You're all set ✅",
                 "Thanks for subscribing — your plan is now active and ready to use.", "Manage subscription"),
            SubscriptionEmailKind.PaymentSucceeded =>
                ("Payment received", "Payment successful",
                 "We've received your payment. Thank you!", "View billing"),
            SubscriptionEmailKind.PaymentFailed =>
                ("Action needed: payment failed", "We couldn't process your payment",
                 "Your latest payment failed. Please update your card to avoid interruption.", "Update payment method"),
            SubscriptionEmailKind.Renewed =>
                ("Your subscription renewed", "Subscription renewed",
                 "Your Fieldore subscription has renewed for another billing period.", "View billing"),
            SubscriptionEmailKind.Cancelled =>
                ("Your subscription was cancelled", "Subscription cancelled",
                 "Your subscription has been cancelled. We're sorry to see you go.", "Reactivate"),
            SubscriptionEmailKind.Expired =>
                ("Your subscription has expired", "Subscription expired",
                 "Your access has ended. Resubscribe any time to pick up where you left off.", "Choose a plan"),
            SubscriptionEmailKind.CardExpiring =>
                ("Your card is expiring soon", "Update your card",
                 "The card on file is about to expire. Update it to keep your subscription active.", "Update card"),
            _ => ("Fieldore", "Fieldore", "", "Open Fieldore"),
        };

        var body = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:480px;margin:0 auto;padding:32px">
              <h1 style="font-size:20px;color:#0f172a;margin:0 0 12px">{heading}</h1>
              <p style="font-size:14px;color:#475569;line-height:1.6">Hi {System.Net.WebUtility.HtmlEncode(businessName)},</p>
              <p style="font-size:14px;color:#475569;line-height:1.6">{message}</p>
              <a href="{portalUrl}" style="display:inline-block;margin-top:16px;background:#2563eb;color:#fff;text-decoration:none;font-weight:700;font-size:14px;padding:12px 20px;border-radius:10px">{cta}</a>
              <p style="font-size:12px;color:#94a3b8;margin-top:32px">— The Fieldore team</p>
            </div>
            """;

        return (subject, body);
    }
}
