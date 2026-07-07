using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Admin;

public sealed class AdminAnalyticsService(FieldoreDbContext dbContext) : IAdminAnalyticsService
{
    public async Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync(CancellationToken ct = default)
    {
        var subs = await dbContext.BusinessSubscriptions.AsNoTracking().ToListAsync(ct);

        int CountBy(string status) => subs.Count(s => s.Status == status);

        var active = subs.Where(s => SubscriptionStatuses.GrantsAccess(s.Status)).ToList();

        // Price lookup keyed by (planId, billingCycle) for revenue math.
        var priceLookup = await dbContext.PlanPrices.AsNoTracking()
            .Where(p => p.PlanId != null)
            .ToDictionaryAsync(p => (p.PlanId, p.BillingCycle), p => p.Amount, ct);

        decimal mrr = 0m, monthlyRevenue = 0m, halfYearlyRevenue = 0m;
        foreach (var s in active)
        {
            if (s.PlanId is null) continue;
            if (!priceLookup.TryGetValue((s.PlanId.Value, s.BillingCycle), out var amount)) continue;

            var months = BillingCycles.MonthsFor(s.BillingCycle);
            mrr += months > 0 ? amount / months : amount;

            if (s.BillingCycle == BillingCycles.Monthly) monthlyRevenue += amount;
            else if (s.BillingCycle == BillingCycles.HalfYearly) halfYearlyRevenue += amount;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var renewalsThisMonth = subs.Count(s => s.RenewsOn is DateOnly r && r >= monthStart && r <= monthEnd);

        var planNames = await dbContext.SubscriptionPlans.AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var distribution = active
            .Where(s => s.PlanId != null)
            .GroupBy(s => s.PlanId!.Value)
            .Select(g => new PlanDistributionDto(planNames.GetValueOrDefault(g.Key, "Unknown"), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var response = new AdminDashboardResponse(
            TotalSubscribers: subs.Count,
            ActiveSubscribers: CountBy(SubscriptionStatuses.Active),
            TrialUsers: CountBy(SubscriptionStatuses.Trial),
            PastDue: CountBy(SubscriptionStatuses.PastDue),
            Cancelled: CountBy(SubscriptionStatuses.Cancelled),
            Expired: CountBy(SubscriptionStatuses.Expired),
            Mrr: Math.Round(mrr, 2),
            Arr: Math.Round(mrr * 12, 2),
            MonthlyRevenue: Math.Round(monthlyRevenue, 2),
            HalfYearlyRevenue: Math.Round(halfYearlyRevenue, 2),
            RenewalsThisMonth: renewalsThisMonth,
            PlanDistribution: distribution);

        return ApiResponse<AdminDashboardResponse>.Create(response, true, null, 200);
    }
}
