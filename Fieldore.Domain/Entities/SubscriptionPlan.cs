namespace Fieldore.Domain.Entities;

/// <summary>
/// An admin-configurable subscription plan (e.g. Starter, Professional). All pricing,
/// limits and features are data-driven — nothing about a plan is hardcoded in app code.
/// </summary>
public sealed class SubscriptionPlan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public bool IsRecommended { get; set; }
    /// <summary>public | hidden</summary>
    public string Visibility { get; set; } = "public";
    public int DisplayOrder { get; set; }

    public string? Badge { get; set; }            // "Popular", "Best Value", "New"…
    public string ButtonText { get; set; } = "Get Started";
    public string? Color { get; set; }            // brand accent for the pricing card
    public int TrialDays { get; set; }

    public List<PlanPrice> Prices { get; set; } = [];
    public List<PlanFeature> Features { get; set; } = [];
}
