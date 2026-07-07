namespace Fieldore.Domain.Constants;

public static class SubscriptionStatuses
{
    public const string Trial = "trial";
    public const string Pending = "pending";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";
    public const string Failed = "failed";

    /// <summary>Statuses that still grant access to paid features.</summary>
    public static readonly string[] AccessGranting = [Trial, Active, PastDue];

    public static bool GrantsAccess(string? status) =>
        status is not null && Array.Exists(AccessGranting, s => s == status);
}
