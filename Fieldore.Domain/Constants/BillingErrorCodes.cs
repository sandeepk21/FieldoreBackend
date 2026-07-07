namespace Fieldore.Domain.Constants;

/// <summary>
/// Stable machine-readable codes prefixed onto API messages so the mobile app / web
/// can detect billing gates (e.g. show an upgrade dialog) rather than parsing prose.
/// Format: "&lt;CODE&gt;: human readable message".
/// </summary>
public static class BillingErrorCodes
{
    public const string SubscriptionLimitReached = "SUBSCRIPTION_LIMIT_REACHED";
    public const string SubscriptionInactive = "SUBSCRIPTION_INACTIVE";

    public static string Message(string code, string message) => $"{code}: {message}";
}
