using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Billing.Entitlements;
using Fieldore.Application.Subscriptions.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Subscriptions;

public sealed class SubscriptionService(FieldoreDbContext dbContext, IEntitlementService entitlements)
    : ISubscriptionService
{
    public async Task<ApiResponse<List<PublicPlanResponse>>> GetPublicPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsArchived && p.Visibility == "public")
            .Include(p => p.Prices.Where(pr => pr.IsActive))
            .Include(p => p.Features.Where(f => f.ShowOnPricing))
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);

        var result = plans.Select(p => new PublicPlanResponse(
            p.Id,
            p.Name,
            p.Slug,
            p.Description,
            p.Currency,
            p.Badge,
            p.IsRecommended,
            p.ButtonText,
            p.Color,
            p.TrialDays,
            p.DisplayOrder,
            p.Prices
                .OrderBy(pr => BillingCycles.MonthsFor(pr.BillingCycle))
                .Select(pr => new PlanPriceResponse(pr.Id, pr.BillingCycle, pr.Amount, pr.Currency))
                .ToList(),
            p.Features
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new PlanFeatureResponse(f.FeatureKey, f.IsEnabled, f.LimitValue, f.DisplayLabel, f.DisplayOrder))
                .ToList()))
            .ToList();

        return ApiResponse<List<PublicPlanResponse>>.Create(result, true, null, 200);
    }

    public async Task<ApiResponse<MySubscriptionResponse>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var businessId = await dbContext.Businesses
            .AsNoTracking()
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessId is null)
        {
            return ApiResponse<MySubscriptionResponse>.Create(null, false, "Business not found for user", 404);
        }

        var subscription = await dbContext.BusinessSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId.Value, cancellationToken);

        var entitlement = await entitlements.GetForBusinessAsync(businessId.Value, cancellationToken);

        var jobLimit = entitlement.LimitFor(FeatureKeys.JobLimit);
        var remainingJobs = jobLimit is null
            ? (int?)null
            : Math.Max(0, jobLimit.Value - entitlement.Usage.CompletedJobsCount);

        var usage = new UsageResponse(
            entitlement.Usage.CompletedJobsCount,
            jobLimit,
            remainingJobs,
            entitlement.Usage.InvoicesCreatedCount,
            entitlement.Usage.CustomersAddedCount,
            entitlement.Usage.EmployeesCount);

        var features = entitlement.Features.Values
            .Select(f => new FeatureStateResponse(f.FeatureKey, f.Enabled, f.Limit, f.Label))
            .ToList();

        var planSlug = subscription?.PlanId is not null
            ? await dbContext.SubscriptionPlans.AsNoTracking()
                .Where(p => p.Id == subscription.PlanId)
                .Select(p => p.Slug)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var response = new MySubscriptionResponse(
            entitlement.PlanName,
            planSlug,
            entitlement.Status,
            entitlement.BillingCycle,
            entitlement.IsActive,
            subscription?.RenewsOn,
            subscription?.TrialEndsOn,
            usage,
            features);

        return ApiResponse<MySubscriptionResponse>.Create(response, true, null, 200);
    }
}
