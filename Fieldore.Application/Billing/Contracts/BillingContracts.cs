using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Billing.Contracts;

public sealed record CheckoutSessionRequest(Guid PlanPriceId, string? SuccessUrl, string? CancelUrl);
public sealed record PortalSessionRequest(string? ReturnUrl);
public sealed record BillingSessionResponse(string Url);

public interface IBillingService
{
    /// <summary>Ensure a plan's active prices exist as Stripe Products/Prices (stores price ids).</summary>
    Task SyncPlanToStripeAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Create a Stripe Checkout Session (mode=subscription) for the signed-in business.</summary>
    Task<ApiResponse<BillingSessionResponse>> CreateCheckoutSessionAsync(
        Guid userId, CheckoutSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Create a Stripe Billing Portal session (manage card, cancel, invoices).</summary>
    Task<ApiResponse<BillingSessionResponse>> CreatePortalSessionAsync(
        Guid userId, PortalSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verify + process a Stripe Billing webhook payload (idempotent).</summary>
    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
