using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Stripe.Contracts;
using Fieldore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.API.Controllers;

[ApiController]
[Route("api/stripe")]
public sealed class StripeConnectController(IStripeService stripeService, FieldoreDbContext dbContext) : ControllerBase
{
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<StripeStatusResult>>> GetStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<StripeStatusResult>.Create(null, false, "Invalid token", 401));
        var result = await stripeService.GetStatusAsync(userId, cancellationToken);
        return ApiResponse<StripeStatusResult>.Create(result, true, "Stripe status retrieved", 200);
    }

    [HttpPost("connect/onboarding")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<StripeOnboardingResult>>> StartOnboarding(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<StripeOnboardingResult>.Create(null, false, "Invalid token", 401));

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        // Pass userId in return/refresh URLs so the browser callback can identify the user without JWT
        var returnUrl  = $"{baseUrl}/api/stripe/connect/return?uid={userId}";
        var refreshUrl = $"{baseUrl}/api/stripe/connect/refresh?uid={userId}";

        try
        {
            var result = await stripeService.CreateOnboardingLinkAsync(userId, returnUrl, refreshUrl, cancellationToken);
            return ApiResponse<StripeOnboardingResult>.Create(result, true, "Onboarding link created", 200);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<StripeOnboardingResult>.Create(null, false, ex.Message, 500));
        }
    }

    [HttpGet("connect/return")]
    [AllowAnonymous]
    public async Task<IActionResult> OnboardingReturn([FromQuery] Guid uid, CancellationToken cancellationToken)
    {
        if (uid != Guid.Empty)
            await stripeService.HandleOnboardingReturnAsync(uid, cancellationToken);

        return Content(OnboardingReturnHtml(), "text/html");
    }

    [HttpGet("connect/refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> OnboardingRefresh([FromQuery] Guid uid, CancellationToken cancellationToken)
    {
        if (uid == Guid.Empty) return BadRequest("Missing uid");

        var baseUrl    = $"{Request.Scheme}://{Request.Host}";
        var returnUrl  = $"{baseUrl}/api/stripe/connect/return?uid={uid}";
        var refreshUrl = $"{baseUrl}/api/stripe/connect/refresh?uid={uid}";

        var result = await stripeService.CreateOnboardingLinkAsync(uid, returnUrl, refreshUrl, cancellationToken);
        if (result.OnboardingUrl is not null)
            return Redirect(result.OnboardingUrl);

        return Content(OnboardingReturnHtml(), "text/html");
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await stripeService.HandleWebhookAsync(payload, signature, cancellationToken);
        return Ok();
    }

    [HttpPost("invoice/{token:guid}/checkout")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateInvoiceCheckout(Guid token, CancellationToken cancellationToken)
    {
        // Used by the public invoice page
        var data = await GetPublicInvoiceDataAsync(token, cancellationToken);
        if (data is null) return NotFound();

        if (string.IsNullOrEmpty(data.StripeAccountId))
            return BadRequest("Business has not connected Stripe");

        if (data.BalanceDue <= 0)
            return BadRequest("Invoice is already paid");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = $"{baseUrl}/invoice/{token}?paid=1";
        var cancelUrl = $"{baseUrl}/invoice/{token}";

        var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
            data.InvoiceId, token.ToString(), data.BalanceDue,
            data.Currency, data.BusinessName, data.InvoiceNumber,
            successUrl, cancelUrl, data.StripeAccountId, cancellationToken);

        if (checkoutUrl is null) return StatusCode(500, "Could not create checkout session");
        return Redirect(checkoutUrl);
    }

    private async Task<PublicInvoiceCheckoutData?> GetPublicInvoiceDataAsync(Guid token, CancellationToken ct)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicToken == token, ct);
        if (invoice is null) return null;

        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoice.BusinessId, ct);

        return new PublicInvoiceCheckoutData(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.BalanceDueAmount,
            business?.Currency ?? "USD",
            business?.Name ?? "",
            business?.StripeOnboardingComplete == true ? business.StripeAccountId : null);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }

    private static string OnboardingReturnHtml() => """
        <!doctype html>
        <html lang='en'><head><meta charset='utf-8'>
        <meta name='viewport' content='width=device-width,initial-scale=1'>
        <title>Stripe Connected</title>
        <style>
          body{margin:0;font-family:-apple-system,Segoe UI,sans-serif;background:#f1f5f9;display:flex;align-items:center;justify-content:center;min-height:100vh}
          .card{background:#fff;border-radius:20px;padding:40px 32px;max-width:360px;text-align:center;box-shadow:0 8px 30px rgba(0,0,0,.08)}
          .icon{width:64px;height:64px;background:#ecfdf5;border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 16px;font-size:32px}
          h1{font-size:22px;color:#0f172a;margin:0 0 8px}
          p{color:#64748b;font-size:14px;margin:0}
        </style></head>
        <body><div class='card'>
          <div class='icon'>✓</div>
          <h1>Stripe Connected!</h1>
          <p>Your Stripe account is now linked. You can close this page and return to the app.</p>
        </div></body></html>
        """;

    private sealed record PublicInvoiceCheckoutData(
        Guid InvoiceId, string InvoiceNumber, decimal BalanceDue,
        string Currency, string BusinessName, string? StripeAccountId);
}
