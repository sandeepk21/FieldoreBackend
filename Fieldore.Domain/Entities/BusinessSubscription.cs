using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class BusinessSubscription : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Provider { get; set; } = "stripe";
    public string? ProviderSubscriptionId { get; set; }

    /// <summary>Kept for display fallback; the authoritative plan is <see cref="PlanId"/>.</summary>
    public string PlanName { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "monthly";
    public string Status { get; set; } = SubscriptionStatuses.Trial;
    public DateOnly? RenewsOn { get; set; }
    public DateOnly? TrialEndsOn { get; set; }

    // ─── Plan link ──────────────────────────────────────────────────────────
    public Guid? PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }
    public Guid? PlanPriceId { get; set; }

    // ─── Stripe Billing (platform account — separate from Connect) ──────────
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    // ─── Current period / lifecycle ─────────────────────────────────────────
    public DateTimeOffset? CurrentPeriodStart { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
