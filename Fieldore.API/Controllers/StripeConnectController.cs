using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Stripe.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Route("api/stripe")]
public sealed class StripeConnectController(IStripeService stripeService) : ControllerBase
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
        var returnUrl = $"{baseUrl}/api/stripe/connect/return";
        var refreshUrl = $"{baseUrl}/api/stripe/connect/refresh";

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
    [Authorize]
    public async Task<IActionResult> OnboardingReturn(CancellationToken cancellationToken)
    {
        if (TryGetUserId(out var userId))
        {
            await stripeService.HandleOnboardingReturnAsync(userId, cancellationToken);
        }
        return Content("<html><body><h2>Stripe connected! You can return to the app.</h2></body></html>", "text/html");
    }

    [HttpGet("connect/refresh")]
    [Authorize]
    public async Task<IActionResult> OnboardingRefresh(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var returnUrl = $"{baseUrl}/api/stripe/connect/return";
        var refreshUrl = $"{baseUrl}/api/stripe/connect/refresh";
        var result = await stripeService.CreateOnboardingLinkAsync(userId, returnUrl, refreshUrl, cancellationToken);
        if (result.OnboardingUrl is not null)
            return Redirect(result.OnboardingUrl);
        return Content("<html><body><h2>Stripe already connected.</h2></body></html>", "text/html");
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
        // find invoice by public token stored on linked estimate? No — invoices have no token.
        // Use a separate lookup: invoice public token (we'll store token on Invoice later)
        // For now, not yet implemented; will be wired via PublicController later
        return null;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }

    private sealed record PublicInvoiceCheckoutData(
        Guid InvoiceId, string InvoiceNumber, decimal BalanceDue,
        string Currency, string BusinessName, string? StripeAccountId);
}
