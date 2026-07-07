namespace Fieldore.Application.Subscriptions.Contracts;

// ─── Public plans (marketing pricing page) ──────────────────────────────────────
public sealed record PlanPriceResponse(Guid Id, string BillingCycle, decimal Amount, string Currency);

public sealed record PlanFeatureResponse(
    string FeatureKey,
    bool IsEnabled,
    int? LimitValue,
    string? Label,
    int DisplayOrder);

public sealed record PublicPlanResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Currency,
    string? Badge,
    bool IsRecommended,
    string ButtonText,
    string? Color,
    int TrialDays,
    int DisplayOrder,
    List<PlanPriceResponse> Prices,
    List<PlanFeatureResponse> Features);

// ─── "My subscription" (mobile app + customer portal) ───────────────────────────
public sealed record UsageResponse(
    int CompletedJobs,
    int? JobLimit,        // null = unlimited
    int? RemainingJobs,   // null = unlimited
    int InvoicesCreated,
    int CustomersAdded,
    int Employees);

public sealed record FeatureStateResponse(string FeatureKey, bool Enabled, int? Limit, string? Label);

public sealed record MySubscriptionResponse(
    string PlanName,
    string? PlanSlug,
    string Status,
    string BillingCycle,
    bool IsActive,
    DateOnly? RenewsOn,
    DateOnly? TrialEndsOn,
    UsageResponse Usage,
    List<FeatureStateResponse> Features);
