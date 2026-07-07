using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Admin;

public sealed class AdminSubscriptionService(FieldoreDbContext dbContext) : IAdminSubscriptionService
{
    public async Task<ApiResponse<List<AdminSubscriptionResponse>>> ListAsync(string? status, CancellationToken ct = default)
    {
        var query =
            from s in dbContext.BusinessSubscriptions.AsNoTracking()
            join b in dbContext.Businesses.AsNoTracking() on s.BusinessId equals b.Id
            select new { s, b.Name };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.s.Status == status);

        var rows = await query.OrderByDescending(x => x.s.UpdatedAt).ToListAsync(ct);
        var result = rows.Select(x => Map(x.s, x.Name)).ToList();
        return ApiResponse<List<AdminSubscriptionResponse>>.Create(result, true, null, 200);
    }

    public async Task<ApiResponse<AdminSubscriptionResponse>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var (sub, name) = await LoadAsync(businessId, ct);
        return sub is null ? NotFound() : Ok(Map(sub, name));
    }

    public async Task<ApiResponse<AdminSubscriptionResponse>> AssignAsync(Guid businessId, AssignPlanRequest request, CancellationToken ct = default)
    {
        var name = await BusinessNameAsync(businessId, ct);
        if (name is null) return NotFound();

        var plan = await dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);
        if (plan is null) return Bad("Plan not found");

        var sub = await GetOrCreateAsync(businessId, ct);
        var price = await dbContext.PlanPrices
            .FirstOrDefaultAsync(p => p.PlanId == request.PlanId && p.BillingCycle == request.BillingCycle, ct);

        var now = DateTimeOffset.UtcNow;
        var months = BillingCycles.MonthsFor(request.BillingCycle);
        sub.PlanId = plan.Id;
        sub.PlanPriceId = price?.Id;
        sub.PlanName = plan.Name;
        sub.BillingCycle = request.BillingCycle;
        sub.Status = SubscriptionStatuses.Active;
        sub.CurrentPeriodStart = now;
        sub.CurrentPeriodEnd = now.AddMonths(months);
        sub.RenewsOn = DateOnly.FromDateTime(now.AddMonths(months).UtcDateTime);
        sub.CancelAtPeriodEnd = false;
        sub.CancelledAt = null;
        sub.EndedAt = null;

        await dbContext.SaveChangesAsync(ct);
        return Ok(Map(sub, name));
    }

    public Task<ApiResponse<AdminSubscriptionResponse>> ChangePlanAsync(Guid businessId, AssignPlanRequest request, CancellationToken ct = default)
        => AssignAsync(businessId, request, ct);

    public async Task<ApiResponse<AdminSubscriptionResponse>> CancelAsync(Guid businessId, CancelSubscriptionRequest request, CancellationToken ct = default)
        => await MutateAsync(businessId, sub =>
        {
            if (request.Immediate)
            {
                sub.Status = SubscriptionStatuses.Cancelled;
                sub.CancelledAt = DateTimeOffset.UtcNow;
                sub.EndedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                sub.CancelAtPeriodEnd = true;
                sub.CancelledAt = DateTimeOffset.UtcNow;
            }
        }, ct);

    public async Task<ApiResponse<AdminSubscriptionResponse>> ResumeAsync(Guid businessId, CancellationToken ct = default)
        => await MutateAsync(businessId, sub =>
        {
            sub.Status = SubscriptionStatuses.Active;
            sub.CancelAtPeriodEnd = false;
            sub.CancelledAt = null;
            sub.EndedAt = null;
        }, ct);

    public async Task<ApiResponse<AdminSubscriptionResponse>> PauseAsync(Guid businessId, CancellationToken ct = default)
        => await MutateAsync(businessId, sub => sub.Status = SubscriptionStatuses.Suspended, ct);

    public async Task<ApiResponse<AdminSubscriptionResponse>> ExtendAsync(Guid businessId, ExtendSubscriptionRequest request, CancellationToken ct = default)
        => await MutateAsync(businessId, sub =>
        {
            var baseEnd = sub.CurrentPeriodEnd ?? DateTimeOffset.UtcNow;
            sub.CurrentPeriodEnd = baseEnd.AddDays(request.Days);
            sub.RenewsOn = DateOnly.FromDateTime(sub.CurrentPeriodEnd.Value.UtcDateTime);
            if (sub.Status is SubscriptionStatuses.Expired or SubscriptionStatuses.PastDue)
                sub.Status = SubscriptionStatuses.Active;
        }, ct);

    public async Task<ApiResponse<AdminSubscriptionResponse>> ExpireAsync(Guid businessId, CancellationToken ct = default)
        => await MutateAsync(businessId, sub =>
        {
            sub.Status = SubscriptionStatuses.Expired;
            sub.EndedAt = DateTimeOffset.UtcNow;
        }, ct);

    // ─── Helpers ────────────────────────────────────────────────────────────
    private async Task<ApiResponse<AdminSubscriptionResponse>> MutateAsync(
        Guid businessId, Action<BusinessSubscription> mutate, CancellationToken ct)
    {
        var (sub, name) = await LoadTrackedAsync(businessId, ct);
        if (sub is null) return NotFound();
        mutate(sub);
        await dbContext.SaveChangesAsync(ct);
        return Ok(Map(sub, name));
    }

    private async Task<BusinessSubscription> GetOrCreateAsync(Guid businessId, CancellationToken ct)
    {
        var sub = await dbContext.BusinessSubscriptions.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);
        if (sub is null)
        {
            sub = new BusinessSubscription { BusinessId = businessId };
            dbContext.BusinessSubscriptions.Add(sub);
        }
        return sub;
    }

    private async Task<(BusinessSubscription? Sub, string Name)> LoadAsync(Guid businessId, CancellationToken ct)
    {
        var sub = await dbContext.BusinessSubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var name = await BusinessNameAsync(businessId, ct);
        return (sub, name ?? "");
    }

    private async Task<(BusinessSubscription? Sub, string Name)> LoadTrackedAsync(Guid businessId, CancellationToken ct)
    {
        var sub = await dbContext.BusinessSubscriptions.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);
        var name = await BusinessNameAsync(businessId, ct);
        return (sub, name ?? "");
    }

    private async Task<string?> BusinessNameAsync(Guid businessId, CancellationToken ct) =>
        await dbContext.Businesses.AsNoTracking().Where(b => b.Id == businessId).Select(b => b.Name).FirstOrDefaultAsync(ct);

    private static AdminSubscriptionResponse Map(BusinessSubscription s, string businessName) => new(
        s.Id, s.BusinessId, businessName, s.PlanId, s.PlanName, s.Status, s.BillingCycle,
        s.RenewsOn, s.TrialEndsOn, s.CurrentPeriodEnd, s.CancelAtPeriodEnd, s.StripeSubscriptionId);

    private static ApiResponse<AdminSubscriptionResponse> Ok(AdminSubscriptionResponse d) => ApiResponse<AdminSubscriptionResponse>.Create(d, true, null, 200);
    private static ApiResponse<AdminSubscriptionResponse> NotFound() => ApiResponse<AdminSubscriptionResponse>.Create(null, false, "Subscription/business not found", 404);
    private static ApiResponse<AdminSubscriptionResponse> Bad(string m) => ApiResponse<AdminSubscriptionResponse>.Create(null, false, m, 400);
}
