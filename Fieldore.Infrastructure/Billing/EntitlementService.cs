using Fieldore.Application.Billing.Entitlements;
using Fieldore.Domain.Constants;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Billing;

public sealed class EntitlementService(FieldoreDbContext dbContext) : IEntitlementService
{
    public async Task<EntitlementSet> GetForBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.BusinessSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);

        if (subscription is null)
        {
            return EntitlementSet.Locked();
        }

        var isActive = SubscriptionStatuses.GrantsAccess(subscription.Status);

        var features = new Dictionary<string, FeatureEntitlement>(StringComparer.Ordinal);
        if (subscription.PlanId is Guid planId)
        {
            var rows = await dbContext.PlanFeatures
                .AsNoTracking()
                .Where(x => x.PlanId == planId)
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                features[row.FeatureKey] = new FeatureEntitlement(
                    row.FeatureKey, row.IsEnabled, row.LimitValue, row.DisplayLabel);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usageRow = await dbContext.SubscriptionUsages
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.PeriodStart <= today && x.PeriodEnd >= today)
            .OrderByDescending(x => x.PeriodStart)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.SubscriptionUsages
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId)
                .OrderByDescending(x => x.PeriodStart)
                .FirstOrDefaultAsync(cancellationToken);

        var usage = usageRow is null
            ? UsageSnapshot.Empty
            : new UsageSnapshot(
                usageRow.CompletedJobsCount,
                usageRow.InvoicesCreatedCount,
                usageRow.CustomersAddedCount,
                usageRow.EmployeesCount,
                usageRow.StorageUsedBytes);

        return new EntitlementSet
        {
            PlanId = subscription.PlanId,
            PlanName = string.IsNullOrWhiteSpace(subscription.PlanName) ? "Free" : subscription.PlanName,
            Status = subscription.Status,
            BillingCycle = subscription.BillingCycle,
            IsActive = isActive,
            Features = features,
            Usage = usage,
        };
    }
}
