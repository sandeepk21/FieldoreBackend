using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Billing.Contracts;
using Fieldore.Application.Notifications;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using BillingCycles = Fieldore.Domain.Constants.BillingCycles;
using SubscriptionStatuses = Fieldore.Domain.Constants.SubscriptionStatuses;

namespace Fieldore.Infrastructure.Billing;

/// <summary>
/// Platform subscription billing (provider → Fieldore). Operates on the platform
/// Stripe account (no connected-account request options). Separate from the Connect
/// <c>StripeService</c> which handles provider → customer invoice payments.
/// </summary>
public sealed class StripeBillingService(
    FieldoreDbContext dbContext,
    IConfiguration configuration,
    ISubscriptionNotifier notifier)
    : IBillingService
{
    private const string MetaBusinessId = "business_id";
    private const string MetaPlanId = "plan_id";
    private const string MetaPlanPriceId = "plan_price_id";

    private string SecretKey => configuration["Stripe:SecretKey"]
        ?? throw new InvalidOperationException("Stripe:SecretKey not configured");
    private string BillingWebhookSecret => configuration["Stripe:BillingWebhookSecret"] ?? string.Empty;
    private string WebBaseUrl => (configuration["Web:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

    // ─── Plan → Stripe sync ─────────────────────────────────────────────────
    public async Task SyncPlanToStripeAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var plan = await dbContext.SubscriptionPlans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan is null) return;

        var priceService = new PriceService();
        string? productId = null;

        // Reuse the product from any already-synced price on this plan.
        var synced = plan.Prices.FirstOrDefault(x => !string.IsNullOrEmpty(x.StripePriceId));
        if (synced is not null)
        {
            var existing = await priceService.GetAsync(synced.StripePriceId, cancellationToken: cancellationToken);
            productId = existing.ProductId;
        }

        foreach (var price in plan.Prices.Where(x => x.IsActive && string.IsNullOrEmpty(x.StripePriceId)))
        {
            if (productId is null)
            {
                var product = await new ProductService().CreateAsync(new ProductCreateOptions
                {
                    Name = plan.Name,
                    Description = plan.Description,
                    Metadata = new Dictionary<string, string> { [MetaPlanId] = plan.Id.ToString() },
                }, cancellationToken: cancellationToken);
                productId = product.Id;
            }

            var created = await priceService.CreateAsync(new PriceCreateOptions
            {
                Product = productId,
                Currency = price.Currency.ToLowerInvariant(),
                UnitAmount = (long)(price.Amount * 100),
                Recurring = new PriceRecurringOptions
                {
                    Interval = "month",
                    IntervalCount = BillingCycles.MonthsFor(price.BillingCycle),
                },
                Metadata = new Dictionary<string, string>
                {
                    [MetaPlanId] = plan.Id.ToString(),
                    [MetaPlanPriceId] = price.Id.ToString(),
                },
            }, cancellationToken: cancellationToken);

            price.StripePriceId = created.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // ─── Checkout (subscription mode) ───────────────────────────────────────
    public async Task<ApiResponse<BillingSessionResponse>> CreateCheckoutSessionAsync(
        Guid userId, CheckoutSessionRequest request, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
        if (business is null)
            return ApiResponse<BillingSessionResponse>.Create(null, false, "Business not found for user", 404);

        var price = await dbContext.PlanPrices
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == request.PlanPriceId && p.IsActive, cancellationToken);
        if (price?.Plan is null)
            return ApiResponse<BillingSessionResponse>.Create(null, false, "Plan price not found", 404);

        // Ensure the price exists in Stripe.
        if (string.IsNullOrEmpty(price.StripePriceId))
        {
            await SyncPlanToStripeAsync(price.PlanId, cancellationToken);
            price = await dbContext.PlanPrices.Include(p => p.Plan)
                .FirstAsync(p => p.Id == request.PlanPriceId, cancellationToken);
        }
        if (string.IsNullOrEmpty(price.StripePriceId))
            return ApiResponse<BillingSessionResponse>.Create(null, false, "Unable to sync plan price to Stripe", 500);

        var subscription = await GetOrCreateSubscriptionRowAsync(business.Id, cancellationToken);

        // Ensure a Stripe Customer.
        if (string.IsNullOrEmpty(subscription.StripeCustomerId))
        {
            var customer = await new CustomerService().CreateAsync(new CustomerCreateOptions
            {
                Email = business.Email,
                Name = business.Name,
                Metadata = new Dictionary<string, string> { [MetaBusinessId] = business.Id.ToString() },
            }, cancellationToken: cancellationToken);
            subscription.StripeCustomerId = customer.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var metadata = new Dictionary<string, string>
        {
            [MetaBusinessId] = business.Id.ToString(),
            [MetaPlanId] = price.PlanId.ToString(),
            [MetaPlanPriceId] = price.Id.ToString(),
        };

        var session = await new global::Stripe.Checkout.SessionService().CreateAsync(new global::Stripe.Checkout.SessionCreateOptions
        {
            Mode = "subscription",
            Customer = subscription.StripeCustomerId,
            LineItems = [new global::Stripe.Checkout.SessionLineItemOptions { Price = price.StripePriceId, Quantity = 1 }],
            SuccessUrl = request.SuccessUrl ?? $"{WebBaseUrl}/portal?checkout=success",
            CancelUrl = request.CancelUrl ?? $"{WebBaseUrl}/pricing?checkout=cancelled",
            Metadata = metadata,
            SubscriptionData = new global::Stripe.Checkout.SessionSubscriptionDataOptions { Metadata = metadata },
        }, cancellationToken: cancellationToken);

        return ApiResponse<BillingSessionResponse>.Create(new BillingSessionResponse(session.Url), true, null, 200);
    }

    // ─── Billing portal ─────────────────────────────────────────────────────
    public async Task<ApiResponse<BillingSessionResponse>> CreatePortalSessionAsync(
        Guid userId, PortalSessionRequest request, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
        if (business is null)
            return ApiResponse<BillingSessionResponse>.Create(null, false, "Business not found for user", 404);

        var subscription = await dbContext.BusinessSubscriptions
            .FirstOrDefaultAsync(x => x.BusinessId == business.Id, cancellationToken);
        if (subscription is null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            return ApiResponse<BillingSessionResponse>.Create(null, false, "No billing account yet. Subscribe first.", 400);

        var session = await new global::Stripe.BillingPortal.SessionService().CreateAsync(
            new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = subscription.StripeCustomerId,
                ReturnUrl = request.ReturnUrl ?? $"{WebBaseUrl}/portal",
            }, cancellationToken: cancellationToken);

        return ApiResponse<BillingSessionResponse>.Create(new BillingSessionResponse(session.Url), true, null, 200);
    }

    // ─── Webhook ────────────────────────────────────────────────────────────
    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(BillingWebhookSecret)) return;

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, BillingWebhookSecret);
        }
        catch
        {
            return; // invalid signature
        }

        // Idempotency — skip already-seen events.
        if (await dbContext.BillingEvents.AnyAsync(x => x.StripeEventId == stripeEvent.Id, cancellationToken))
            return;

        var log = new BillingEvent { StripeEventId = stripeEvent.Id, Type = stripeEvent.Type, Status = "received" };
        dbContext.BillingEvents.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                {
                    if (stripeEvent.Data.Object is global::Stripe.Checkout.Session session &&
                        session.Mode == "subscription" &&
                        !string.IsNullOrEmpty(session.SubscriptionId))
                    {
                        var sub = await new global::Stripe.SubscriptionService()
                            .GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
                        await UpsertFromStripeSubscriptionAsync(sub, session.Metadata, cancellationToken);
                    }
                    break;
                }
                case "customer.subscription.created":
                case "customer.subscription.updated":
                {
                    if (stripeEvent.Data.Object is Subscription sub)
                        await UpsertFromStripeSubscriptionAsync(sub, sub.Metadata, cancellationToken);
                    break;
                }
                case "customer.subscription.deleted":
                {
                    if (stripeEvent.Data.Object is Subscription sub)
                        await UpsertFromStripeSubscriptionAsync(sub, sub.Metadata, cancellationToken, ended: true);
                    break;
                }
                case "invoice.payment_failed":
                {
                    if (stripeEvent.Data.Object is global::Stripe.Invoice invoice && !string.IsNullOrEmpty(invoice.CustomerId))
                    {
                        var row = await dbContext.BusinessSubscriptions
                            .FirstOrDefaultAsync(x => x.StripeCustomerId == invoice.CustomerId, cancellationToken);
                        if (row is not null)
                        {
                            row.Status = SubscriptionStatuses.PastDue;
                            await dbContext.SaveChangesAsync(cancellationToken);
                            await notifier.NotifyAsync(row.BusinessId, SubscriptionEmailKind.PaymentFailed, cancellationToken);
                        }
                    }
                    break;
                }
            }

            log.Status = "processed";
            log.ProcessedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            log.Status = "failed";
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private async Task<BusinessSubscription> GetOrCreateSubscriptionRowAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var row = await dbContext.BusinessSubscriptions
            .FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken);
        if (row is null)
        {
            row = new BusinessSubscription { BusinessId = businessId, Status = SubscriptionStatuses.Pending };
            dbContext.BusinessSubscriptions.Add(row);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return row;
    }

    private async Task UpsertFromStripeSubscriptionAsync(
        Subscription sub, IDictionary<string, string>? metadata, CancellationToken cancellationToken, bool ended = false)
    {
        BusinessSubscription? row = null;

        if (metadata is not null && metadata.TryGetValue(MetaBusinessId, out var bizStr) && Guid.TryParse(bizStr, out var businessId))
        {
            row = await dbContext.BusinessSubscriptions.FirstOrDefaultAsync(x => x.BusinessId == businessId, cancellationToken)
                  ?? new BusinessSubscription { BusinessId = businessId };
            if (row.Id == Guid.Empty) dbContext.BusinessSubscriptions.Add(row);
        }

        row ??= await dbContext.BusinessSubscriptions.FirstOrDefaultAsync(x => x.StripeSubscriptionId == sub.Id, cancellationToken);
        if (row is null) return; // cannot associate — ignore

        var previousStatus = row.Status;
        row.Provider = "stripe";
        row.StripeSubscriptionId = sub.Id;
        row.StripeCustomerId = sub.CustomerId;
        row.CancelAtPeriodEnd = sub.CancelAtPeriodEnd;
        row.Status = ended ? SubscriptionStatuses.Cancelled : MapStatus(sub.Status);

        var item = sub.Items?.Data?.FirstOrDefault();
        if (item is not null)
        {
            row.CurrentPeriodStart = new DateTimeOffset(DateTime.SpecifyKind(item.CurrentPeriodStart, DateTimeKind.Utc));
            row.CurrentPeriodEnd = new DateTimeOffset(DateTime.SpecifyKind(item.CurrentPeriodEnd, DateTimeKind.Utc));
            row.RenewsOn = DateOnly.FromDateTime(item.CurrentPeriodEnd);

            var stripePriceId = item.Price?.Id;
            if (!string.IsNullOrEmpty(stripePriceId))
            {
                var planPrice = await dbContext.PlanPrices
                    .Include(p => p.Plan)
                    .FirstOrDefaultAsync(p => p.StripePriceId == stripePriceId, cancellationToken);
                if (planPrice is not null)
                {
                    row.PlanId = planPrice.PlanId;
                    row.PlanPriceId = planPrice.Id;
                    row.BillingCycle = planPrice.BillingCycle;
                    row.PlanName = planPrice.Plan?.Name ?? row.PlanName;
                }
            }
        }

        if (sub.TrialEnd is DateTime trialEnd)
            row.TrialEndsOn = DateOnly.FromDateTime(trialEnd);

        if (ended)
            row.EndedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Lifecycle emails (best-effort — notifier never throws).
        if (ended)
            await notifier.NotifyAsync(row.BusinessId, SubscriptionEmailKind.Cancelled, cancellationToken);
        else if (row.Status == SubscriptionStatuses.Active && previousStatus != SubscriptionStatuses.Active)
            await notifier.NotifyAsync(row.BusinessId, SubscriptionEmailKind.Activated, cancellationToken);
    }

    private static string MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "trialing" => SubscriptionStatuses.Trial,
        "active" => SubscriptionStatuses.Active,
        "past_due" => SubscriptionStatuses.PastDue,
        "canceled" => SubscriptionStatuses.Cancelled,
        "unpaid" => SubscriptionStatuses.Suspended,
        "paused" => SubscriptionStatuses.Suspended,
        "incomplete" => SubscriptionStatuses.Pending,
        "incomplete_expired" => SubscriptionStatuses.Failed,
        _ => SubscriptionStatuses.Pending,
    };
}
