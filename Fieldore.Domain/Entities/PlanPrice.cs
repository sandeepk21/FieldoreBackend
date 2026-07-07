namespace Fieldore.Domain.Entities;

/// <summary>
/// A price point for a plan at a given billing cycle (e.g. Starter / monthly / $29).
/// Maps 1:1 to a Stripe Price (<see cref="StripePriceId"/>) once synced.
/// </summary>
public sealed class PlanPrice : AuditableEntity
{
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }

    /// <summary>See <c>BillingCycles</c> — monthly, half_yearly, …</summary>
    public string BillingCycle { get; set; } = "monthly";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public string? StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;
}
