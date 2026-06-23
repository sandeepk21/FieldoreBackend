using Fieldore.Application.Stripe.Contracts;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Fieldore.Infrastructure.Stripe;

public sealed class StripeService(FieldoreDbContext dbContext, IConfiguration configuration) : IStripeService
{
    private string SecretKey => configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey not configured");
    private string WebhookSecret => configuration["Stripe:WebhookSecret"] ?? string.Empty;

    public async Task<StripeStatusResult> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
        if (business is null) return new StripeStatusResult(false, false, null);
        return new StripeStatusResult(
            !string.IsNullOrEmpty(business.StripeAccountId),
            business.StripeOnboardingComplete,
            business.StripeAccountId);
    }

    public async Task<StripeOnboardingResult> CreateOnboardingLinkAsync(
        Guid userId, string returnUrl, string refreshUrl, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
        if (business is null) throw new InvalidOperationException("Business not found");

        // Create account if not yet
        if (string.IsNullOrEmpty(business.StripeAccountId))
        {
            var accountService = new AccountService();
            var account = await accountService.CreateAsync(new AccountCreateOptions
            {
                Type = "express",
                Country = "US",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                },
            });
            business.StripeAccountId = account.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (business.StripeOnboardingComplete)
            return new StripeOnboardingResult(true, null, business.StripeAccountId);

        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(new AccountLinkCreateOptions
        {
            Account = business.StripeAccountId,
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl,
            Type = "account_onboarding",
        });

        return new StripeOnboardingResult(false, link.Url, business.StripeAccountId);
    }

    public async Task HandleOnboardingReturnAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
        if (business is null || string.IsNullOrEmpty(business.StripeAccountId)) return;

        var accountService = new AccountService();
        var account = await accountService.GetAsync(business.StripeAccountId);
        if (account.DetailsSubmitted)
        {
            business.StripeOnboardingComplete = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string?> CreateCheckoutSessionAsync(
        Guid invoiceId, string token, decimal amount, string currency,
        string businessName, string invoiceNumber,
        string successUrl, string cancelUrl, string stripeAccountId,
        CancellationToken cancellationToken = default)
    {
        StripeConfiguration.ApiKey = SecretKey;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency.ToLowerInvariant(),
                        UnitAmount = (long)(amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Invoice {invoiceNumber}",
                            Description = $"Payment to {businessName}",
                        },
                    },
                    Quantity = 1,
                },
            ],
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "invoice_id", invoiceId.ToString() },
                { "invoice_token", token },
            },
        };

        var requestOptions = new RequestOptions { StripeAccount = stripeAccountId };
        var service = new SessionService();
        var session = await service.CreateAsync(options, requestOptions);
        return session.Url;
    }

    public async Task HandleWebhookAsync(string payload, string stripeSignature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(WebhookSecret)) return;

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, WebhookSecret);
        }
        catch
        {
            return;
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session is null) return;

            if (!session.Metadata.TryGetValue("invoice_id", out var invoiceIdStr)) return;
            if (!Guid.TryParse(invoiceIdStr, out var invoiceId)) return;

            var amountPaid = (session.AmountTotal ?? 0) / 100m;

            var invoice = await dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
            if (invoice is null) return;

            var existing = await dbContext.PaymentRecords
                .AnyAsync(x => x.InvoiceId == invoiceId && x.ReferenceNumber == session.Id, cancellationToken);
            if (existing) return;

            var payment = new Fieldore.Domain.Entities.PaymentRecord
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                Amount = amountPaid,
                Method = "stripe",
                PaidAt = DateTimeOffset.UtcNow,
                ReferenceNumber = session.Id,
                Notes = "Paid online via Stripe",
            };

            dbContext.PaymentRecords.Add(payment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var totalPaid = await dbContext.PaymentRecords
                .Where(x => x.InvoiceId == invoiceId)
                .SumAsync(x => x.Amount, cancellationToken);

            var newBalance = Math.Max(0m, invoice.TotalAmount - totalPaid);
            invoice.BalanceDueAmount = newBalance;
            invoice.Status = newBalance <= 0m ? "paid" : "partially_paid";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
