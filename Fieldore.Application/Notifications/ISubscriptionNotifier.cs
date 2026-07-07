namespace Fieldore.Application.Notifications;

public enum SubscriptionEmailKind
{
    TrialStarted,
    Activated,
    PaymentSucceeded,
    PaymentFailed,
    Renewed,
    Cancelled,
    Expired,
    CardExpiring,
}

/// <summary>
/// High-level subscription lifecycle emails. Resolves the business's email + notification
/// preference, renders a template and sends. Best-effort: never throws to callers.
/// </summary>
public interface ISubscriptionNotifier
{
    Task NotifyAsync(Guid businessId, SubscriptionEmailKind kind, CancellationToken cancellationToken = default);
}
