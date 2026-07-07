using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Billing;

/// <summary>
/// Seeds the two launch plans (Starter, Professional) once, if no plans exist.
/// After seeding, plans/prices/features are fully editable from the admin panel —
/// this only bootstraps an empty install. Idempotent: no-op when plans already exist.
/// </summary>
public static class BillingSeeder
{
    public static async Task SeedAsync(FieldoreDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.SubscriptionPlans.AnyAsync(cancellationToken))
        {
            return;
        }

        db.SubscriptionPlans.AddRange(BuildStarter(), BuildProfessional());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SubscriptionPlan BuildStarter() => new()
    {
        Name = "Starter",
        Slug = "starter",
        Description = "For solo operators getting started.",
        Currency = "USD",
        DisplayOrder = 1,
        ButtonText = "Start Free Trial",
        TrialDays = 14,
        Color = "#2563eb",
        Prices =
        [
            new PlanPrice { BillingCycle = BillingCycles.Monthly, Amount = 29m, Currency = "USD" },
            new PlanPrice { BillingCycle = BillingCycles.HalfYearly, Amount = 159m, Currency = "USD" },
        ],
        Features =
        [
            Feat(FeatureKeys.JobLimit, "5 completed jobs / month", order: 1, limit: 5),
            Feat(FeatureKeys.UnlimitedCustomers, "Unlimited customers", order: 2),
            Feat(FeatureKeys.UnlimitedQuotes, "Unlimited quotes", order: 3),
            Feat(FeatureKeys.UnlimitedInvoices, "Unlimited invoices", order: 4),
            Feat(FeatureKeys.UnlimitedEmployees, "Unlimited employees", order: 5),
            Feat(FeatureKeys.UnlimitedScheduling, "Unlimited scheduling", order: 6),
            Feat(FeatureKeys.PrioritySupport, "Priority support", order: 7, enabled: false),
            Feat(FeatureKeys.CustomBranding, "Custom branding", order: 8, enabled: false),
        ],
    };

    private static SubscriptionPlan BuildProfessional() => new()
    {
        Name = "Professional",
        Slug = "professional",
        Description = "For growing teams that need it all.",
        Currency = "USD",
        DisplayOrder = 2,
        IsRecommended = true,
        Badge = "Popular",
        ButtonText = "Start Free Trial",
        TrialDays = 14,
        Color = "#0f172a",
        Prices =
        [
            new PlanPrice { BillingCycle = BillingCycles.Monthly, Amount = 39m, Currency = "USD" },
            new PlanPrice { BillingCycle = BillingCycles.HalfYearly, Amount = 219m, Currency = "USD" },
        ],
        Features =
        [
            Feat(FeatureKeys.JobLimit, "Unlimited jobs", order: 1),           // limit null = unlimited
            Feat(FeatureKeys.UnlimitedCustomers, "Unlimited customers", order: 2),
            Feat(FeatureKeys.UnlimitedQuotes, "Unlimited quotes", order: 3),
            Feat(FeatureKeys.UnlimitedInvoices, "Unlimited invoices", order: 4),
            Feat(FeatureKeys.UnlimitedEmployees, "Unlimited employees", order: 5),
            Feat(FeatureKeys.UnlimitedScheduling, "Unlimited scheduling", order: 6),
            Feat(FeatureKeys.PrioritySupport, "Priority support", order: 7),
            Feat(FeatureKeys.FutureAiFeatures, "Future features included", order: 8),
        ],
    };

    private static PlanFeature Feat(string key, string label, int order, int? limit = null, bool enabled = true) => new()
    {
        FeatureKey = key,
        DisplayLabel = label,
        DisplayOrder = order,
        LimitValue = limit,
        IsEnabled = enabled,
        ShowOnPricing = true,
    };
}
