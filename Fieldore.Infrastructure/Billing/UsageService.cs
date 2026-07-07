using Fieldore.Application.Billing.Usage;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Billing;

public sealed class UsageService(FieldoreDbContext dbContext) : IUsageService
{
    public async Task IncrementCompletedJobsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var usage = await GetOrCreateCurrentAsync(businessId, cancellationToken);
        usage.CompletedJobsCount += 1;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionUsage> GetOrCreateCurrentAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var (start, end) = await ResolvePeriodAsync(businessId, cancellationToken);

        var usage = await dbContext.SubscriptionUsages
            .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.PeriodStart == start, cancellationToken);

        if (usage is null)
        {
            usage = new SubscriptionUsage { BusinessId = businessId, PeriodStart = start, PeriodEnd = end };
            dbContext.SubscriptionUsages.Add(usage);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return usage;
    }

    private async Task<(DateOnly Start, DateOnly End)> ResolvePeriodAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.BusinessSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);

        if (subscription?.CurrentPeriodStart is DateTimeOffset periodStart &&
            subscription.CurrentPeriodEnd is DateTimeOffset periodEnd)
        {
            return (DateOnly.FromDateTime(periodStart.UtcDateTime), DateOnly.FromDateTime(periodEnd.UtcDateTime));
        }

        // Fallback: calendar month (e.g. trial with no Stripe period yet).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new DateOnly(today.Year, today.Month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }
}
