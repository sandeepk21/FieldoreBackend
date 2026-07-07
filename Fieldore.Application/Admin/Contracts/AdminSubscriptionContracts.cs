using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Admin.Contracts;

public sealed record AdminSubscriptionResponse(
    Guid Id,
    Guid BusinessId,
    string BusinessName,
    Guid? PlanId,
    string? PlanName,
    string Status,
    string BillingCycle,
    DateOnly? RenewsOn,
    DateOnly? TrialEndsOn,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    string? StripeSubscriptionId);

public sealed record AssignPlanRequest(Guid PlanId, string BillingCycle);
public sealed record ExtendSubscriptionRequest(int Days);
public sealed record CancelSubscriptionRequest(bool Immediate);

public interface IAdminSubscriptionService
{
    Task<ApiResponse<List<AdminSubscriptionResponse>>> ListAsync(string? status, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> GetAsync(Guid businessId, CancellationToken ct = default);

    // Manual, local lifecycle actions (no Stripe call — for support/overrides).
    Task<ApiResponse<AdminSubscriptionResponse>> AssignAsync(Guid businessId, AssignPlanRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> ChangePlanAsync(Guid businessId, AssignPlanRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> CancelAsync(Guid businessId, CancelSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> ResumeAsync(Guid businessId, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> PauseAsync(Guid businessId, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> ExtendAsync(Guid businessId, ExtendSubscriptionRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminSubscriptionResponse>> ExpireAsync(Guid businessId, CancellationToken ct = default);
}
