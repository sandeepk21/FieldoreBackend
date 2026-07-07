using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Billing.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/billing")]
public sealed class BillingController(IBillingService billingService) : ControllerBase
{
    /// <summary>Create a Stripe Checkout Session (subscription) — web only.</summary>
    [HttpPost("checkout-session")]
    public async Task<ActionResult<ApiResponse<BillingSessionResponse>>> CreateCheckout(
        [FromBody] CheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<BillingSessionResponse>.Create(null, false, "Invalid token", 401));

        return await billingService.CreateCheckoutSessionAsync(userId, request, cancellationToken);
    }

    /// <summary>Create a Stripe Billing Portal session (manage card, invoices, cancel).</summary>
    [HttpPost("portal-session")]
    public async Task<ActionResult<ApiResponse<BillingSessionResponse>>> CreatePortal(
        [FromBody] PortalSessionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<BillingSessionResponse>.Create(null, false, "Invalid token", 401));

        return await billingService.CreatePortalSessionAsync(userId, request, cancellationToken);
    }

    /// <summary>Stripe Billing webhook. Verifies the signature; idempotent.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        await billingService.HandleWebhookAsync(payload, signature, cancellationToken);
        return Ok();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
