namespace Fieldore.Application.Stripe.Contracts;

public sealed record StripeOnboardingResult(bool AlreadyConnected, string? OnboardingUrl, string? AccountId);
public sealed record StripeStatusResult(bool IsConnected, bool OnboardingComplete, string? AccountId);

public interface IStripeService
{
    Task<StripeStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<StripeOnboardingResult> CreateOnboardingLinkAsync(Guid userId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default);
    Task HandleOnboardingReturnAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> CreateCheckoutSessionAsync(Guid invoiceId, string token, decimal amount, string currency, string businessName, string invoiceNumber, string successUrl, string cancelUrl, string stripeAccountId, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken cancellationToken = default);
}
