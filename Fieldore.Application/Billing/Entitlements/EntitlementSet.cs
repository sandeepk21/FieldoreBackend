using Fieldore.Domain.Constants;

namespace Fieldore.Application.Billing.Entitlements;

public sealed record FeatureEntitlement(string FeatureKey, bool Enabled, int? Limit, string? Label);

public sealed record UsageSnapshot(
    int CompletedJobsCount,
    int InvoicesCreatedCount,
    int CustomersAddedCount,
    int EmployeesCount,
    long StorageUsedBytes)
{
    public static readonly UsageSnapshot Empty = new(0, 0, 0, 0, 0);
}

/// <summary>
/// The resolved entitlements for a business at a point in time: its plan, status,
/// per-feature capabilities/limits, and current usage. Enforcement code asks this
/// object rather than hardcoding plan rules.
/// </summary>
public sealed class EntitlementSet
{
    public Guid? PlanId { get; init; }
    public string? PlanSlug { get; init; }
    public string PlanName { get; init; } = "Free";
    public string Status { get; init; } = SubscriptionStatuses.Expired;
    public string BillingCycle { get; init; } = BillingCycles.Monthly;

    /// <summary>True when the subscription status still grants access to paid features.</summary>
    public bool IsActive { get; init; }

    public IReadOnlyDictionary<string, FeatureEntitlement> Features { get; init; }
        = new Dictionary<string, FeatureEntitlement>();
    public UsageSnapshot Usage { get; init; } = UsageSnapshot.Empty;

    /// <summary>A locked, no-access entitlement for businesses with no active subscription.</summary>
    public static EntitlementSet Locked(string status = SubscriptionStatuses.Expired) =>
        new() { Status = status, IsActive = false };

    /// <summary>Whether a boolean feature is available (requires an active subscription).</summary>
    public bool Can(string featureKey) =>
        IsActive && Features.TryGetValue(featureKey, out var f) && f.Enabled;

    /// <summary>Numeric limit for a feature; null = unlimited (or feature absent).</summary>
    public int? LimitFor(string featureKey) =>
        Features.TryGetValue(featureKey, out var f) ? f.Limit : null;

    /// <summary>Remaining allowance for a limit feature given usage; int.MaxValue = unlimited.</summary>
    public int Remaining(string featureKey, int used)
    {
        var limit = LimitFor(featureKey);
        return limit is null ? int.MaxValue : Math.Max(0, limit.Value - used);
    }

    /// <summary>Remaining completed jobs this period (int.MaxValue = unlimited).</summary>
    public int RemainingCompletedJobs() =>
        Remaining(FeatureKeys.JobLimit, Usage.CompletedJobsCount);
}
