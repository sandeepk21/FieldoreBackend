namespace Fieldore.Domain.Entities;

/// <summary>
/// A single feature/entitlement a plan grants. <see cref="LimitValue"/> is used for
/// numeric-limit features (null = unlimited); <see cref="IsEnabled"/> for boolean
/// capabilities. Adding a feature = new row(s), no schema change.
/// </summary>
public sealed class PlanFeature : AuditableEntity
{
    public Guid PlanId { get; set; }
    public SubscriptionPlan? Plan { get; set; }

    /// <summary>See <c>FeatureKeys</c> — e.g. job_limit, pdf_export, gps_tracking.</summary>
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    /// <summary>Numeric cap for limit-based features; null = unlimited.</summary>
    public int? LimitValue { get; set; }

    public string? DisplayLabel { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>Whether this feature is listed on the marketing pricing card.</summary>
    public bool ShowOnPricing { get; set; } = true;
}
