using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Billing.Contracts;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Admin;

public sealed class AdminPlanService(FieldoreDbContext dbContext, IBillingService billingService) : IAdminPlanService
{
    public async Task<ApiResponse<List<AdminPlanResponse>>> ListAsync(CancellationToken ct = default)
    {
        var plans = await dbContext.SubscriptionPlans
            .AsNoTracking()
            .Include(p => p.Prices)
            .Include(p => p.Features)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(ct);

        var counts = await GetActiveCountsAsync(ct);
        var result = plans.Select(p => Map(p, counts.GetValueOrDefault(p.Id))).ToList();
        return ApiResponse<List<AdminPlanResponse>>.Create(result, true, null, 200);
    }

    public async Task<ApiResponse<AdminPlanResponse>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();
        var counts = await GetActiveCountsAsync(ct);
        return Ok(Map(plan, counts.GetValueOrDefault(plan.Id)));
    }

    public async Task<ApiResponse<AdminPlanResponse>> CreateAsync(CreatePlanRequest request, CancellationToken ct = default)
    {
        var error = ValidateBasics(request.Name, request.Prices);
        if (error is not null) return Bad(error);

        var slug = await UniqueSlugAsync(request.Slug ?? Slugify(request.Name), null, ct);
        var maxOrder = await dbContext.SubscriptionPlans.MaxAsync(p => (int?)p.DisplayOrder, ct) ?? 0;

        var plan = new SubscriptionPlan
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
            Badge = request.Badge,
            ButtonText = string.IsNullOrWhiteSpace(request.ButtonText) ? "Get Started" : request.ButtonText!,
            Color = request.Color,
            TrialDays = request.TrialDays,
            Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? "public" : request.Visibility,
            IsRecommended = request.IsRecommended,
            DisplayOrder = maxOrder + 1,
            Prices = request.Prices.Select(ToPrice).ToList(),
            Features = request.Features.Select(ToFeature).ToList(),
        };

        dbContext.SubscriptionPlans.Add(plan);
        await dbContext.SaveChangesAsync(ct);
        await TrySyncAsync(plan.Id, ct);

        return await GetAsync(plan.Id, ct);
    }

    public async Task<ApiResponse<AdminPlanResponse>> UpdateAsync(Guid id, UpdatePlanRequest request, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return Bad("Name is required");

        plan.Name = request.Name.Trim();
        plan.Description = request.Description;
        plan.Currency = string.IsNullOrWhiteSpace(request.Currency) ? plan.Currency : request.Currency;
        plan.Badge = request.Badge;
        plan.ButtonText = string.IsNullOrWhiteSpace(request.ButtonText) ? plan.ButtonText : request.ButtonText!;
        plan.Color = request.Color;
        plan.TrialDays = request.TrialDays;
        plan.Visibility = string.IsNullOrWhiteSpace(request.Visibility) ? plan.Visibility : request.Visibility;

        if (request.IsRecommended && !plan.IsRecommended)
            await ClearRecommendedAsync(ct);
        plan.IsRecommended = request.IsRecommended;

        await dbContext.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ApiResponse<AdminPlanResponse>> ReplacePricesAsync(Guid id, ReplacePricesRequest request, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();

        // Preserve StripePriceId when an unchanged (cycle+amount) price already exists.
        var existing = plan.Prices.ToList();
        dbContext.PlanPrices.RemoveRange(existing);
        plan.Prices = request.Prices.Select(dto =>
        {
            var match = existing.FirstOrDefault(e => e.BillingCycle == dto.BillingCycle && e.Amount == dto.Amount);
            var price = ToPrice(dto);
            price.StripePriceId = match?.StripePriceId; // reuse only if truly unchanged
            return price;
        }).ToList();

        await dbContext.SaveChangesAsync(ct);
        await TrySyncAsync(id, ct);
        return await GetAsync(id, ct);
    }

    public async Task<ApiResponse<AdminPlanResponse>> ReplaceFeaturesAsync(Guid id, ReplaceFeaturesRequest request, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();

        dbContext.PlanFeatures.RemoveRange(plan.Features);
        plan.Features = request.Features.Select(ToFeature).ToList();
        await dbContext.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ApiResponse<AdminPlanResponse>> SetStateAsync(Guid id, string action, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();

        switch (action?.ToLowerInvariant())
        {
            case "enable": plan.IsActive = true; break;
            case "disable": plan.IsActive = false; break;
            case "archive": plan.IsArchived = true; plan.IsActive = false; break;
            case "unarchive": plan.IsArchived = false; break;
            case "recommend": await ClearRecommendedAsync(ct); plan.IsRecommended = true; break;
            default: return Bad($"Unknown action '{action}'");
        }

        await dbContext.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ApiResponse<AdminPlanResponse>> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();

        var maxOrder = await dbContext.SubscriptionPlans.MaxAsync(p => (int?)p.DisplayOrder, ct) ?? 0;
        var copy = new SubscriptionPlan
        {
            Name = $"{plan.Name} (Copy)",
            Slug = await UniqueSlugAsync(Slugify(plan.Name + "-copy"), null, ct),
            Description = plan.Description,
            Currency = plan.Currency,
            Badge = plan.Badge,
            ButtonText = plan.ButtonText,
            Color = plan.Color,
            TrialDays = plan.TrialDays,
            Visibility = "hidden",
            IsRecommended = false,
            IsActive = false,
            DisplayOrder = maxOrder + 1,
            Prices = plan.Prices.Select(p => new PlanPrice
            {
                BillingCycle = p.BillingCycle, Amount = p.Amount, Currency = p.Currency, IsActive = p.IsActive,
            }).ToList(),
            Features = plan.Features.Select(f => new PlanFeature
            {
                FeatureKey = f.FeatureKey, IsEnabled = f.IsEnabled, LimitValue = f.LimitValue,
                DisplayLabel = f.DisplayLabel, DisplayOrder = f.DisplayOrder, ShowOnPricing = f.ShowOnPricing,
            }).ToList(),
        };

        dbContext.SubscriptionPlans.Add(copy);
        await dbContext.SaveChangesAsync(ct);
        return await GetAsync(copy.Id, ct);
    }

    public async Task<ApiResponse<bool>> ReorderAsync(ReorderPlansRequest request, CancellationToken ct = default)
    {
        var ids = request.Items.Select(i => i.Id).ToList();
        var plans = await dbContext.SubscriptionPlans.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        foreach (var plan in plans)
            plan.DisplayOrder = request.Items.First(i => i.Id == plan.Id).DisplayOrder;

        await dbContext.SaveChangesAsync(ct);
        return ApiResponse<bool>.Create(true, true, null, 200);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return ApiResponse<bool>.Create(false, false, "Plan not found", 404);

        var inUse = await dbContext.BusinessSubscriptions.AnyAsync(s => s.PlanId == id, ct);
        if (inUse)
            return ApiResponse<bool>.Create(false, false, "Cannot delete a plan that has subscriptions. Archive it instead.", 409);

        dbContext.SubscriptionPlans.Remove(plan);
        await dbContext.SaveChangesAsync(ct);
        return ApiResponse<bool>.Create(true, true, null, 200);
    }

    public async Task<ApiResponse<AdminPlanResponse>> SyncToStripeAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await LoadAsync(id, ct);
        if (plan is null) return NotFound();
        await billingService.SyncPlanToStripeAsync(id, ct);
        return await GetAsync(id, ct);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private async Task<SubscriptionPlan?> LoadAsync(Guid id, CancellationToken ct) =>
        await dbContext.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    private async Task<Dictionary<Guid, int>> GetActiveCountsAsync(CancellationToken ct) =>
        await dbContext.BusinessSubscriptions
            .Where(s => s.PlanId != null && (s.Status == "active" || s.Status == "trial" || s.Status == "past_due"))
            .GroupBy(s => s.PlanId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    private async Task ClearRecommendedAsync(CancellationToken ct)
    {
        var current = await dbContext.SubscriptionPlans.Where(p => p.IsRecommended).ToListAsync(ct);
        foreach (var p in current) p.IsRecommended = false;
    }

    private async Task TrySyncAsync(Guid planId, CancellationToken ct)
    {
        try { await billingService.SyncPlanToStripeAsync(planId, ct); }
        catch { /* Stripe not configured / offline — prices lazy-sync at checkout. */ }
    }

    private async Task<string> UniqueSlugAsync(string baseSlug, Guid? excludeId, CancellationToken ct)
    {
        var slug = baseSlug;
        var i = 1;
        while (await dbContext.SubscriptionPlans.AnyAsync(p => p.Slug == slug && p.Id != excludeId, ct))
            slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private static string Slugify(string name) =>
        new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-').Replace("--", "-");

    private static string? ValidateBasics(string name, List<AdminPlanPriceDto> prices)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Name is required";
        if (prices is null || prices.Count == 0) return "At least one price is required";
        if (prices.Any(p => p.Amount < 0)) return "Price amount cannot be negative";
        return null;
    }

    private static PlanPrice ToPrice(AdminPlanPriceDto d) => new()
    {
        BillingCycle = d.BillingCycle, Amount = d.Amount,
        Currency = string.IsNullOrWhiteSpace(d.Currency) ? "USD" : d.Currency, IsActive = d.IsActive,
    };

    private static PlanFeature ToFeature(AdminPlanFeatureDto d) => new()
    {
        FeatureKey = d.FeatureKey, IsEnabled = d.IsEnabled, LimitValue = d.LimitValue,
        DisplayLabel = d.DisplayLabel, DisplayOrder = d.DisplayOrder, ShowOnPricing = d.ShowOnPricing,
    };

    private static AdminPlanResponse Map(SubscriptionPlan p, int activeCount) => new(
        p.Id, p.Name, p.Slug, p.Description, p.Currency,
        p.IsActive, p.IsArchived, p.IsRecommended, p.Visibility, p.DisplayOrder,
        p.Badge, p.ButtonText, p.Color, p.TrialDays, activeCount,
        p.Prices.OrderBy(x => x.Amount)
            .Select(x => new AdminPlanPriceDto(x.Id, x.BillingCycle, x.Amount, x.Currency, x.IsActive, x.StripePriceId)).ToList(),
        p.Features.OrderBy(x => x.DisplayOrder)
            .Select(x => new AdminPlanFeatureDto(x.Id, x.FeatureKey, x.IsEnabled, x.LimitValue, x.DisplayLabel, x.DisplayOrder, x.ShowOnPricing)).ToList());

    private static ApiResponse<AdminPlanResponse> Ok(AdminPlanResponse data) => ApiResponse<AdminPlanResponse>.Create(data, true, null, 200);
    private static ApiResponse<AdminPlanResponse> NotFound() => ApiResponse<AdminPlanResponse>.Create(null, false, "Plan not found", 404);
    private static ApiResponse<AdminPlanResponse> Bad(string msg) => ApiResponse<AdminPlanResponse>.Create(null, false, msg, 400);
}
