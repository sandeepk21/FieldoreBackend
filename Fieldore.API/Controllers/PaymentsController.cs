using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Payments.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpPost("record/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Record(
        Guid invoiceId, [FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<PaymentResponse>.Create(null, false, "Invalid token", 401));
        return await paymentService.RecordAsync(userId, invoiceId, request, cancellationToken);
    }

    [HttpGet("invoice/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<List<PaymentResponse>>>> GetByInvoice(
        Guid invoiceId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<List<PaymentResponse>>.Create(null, false, "Invalid token", 401));
        return await paymentService.GetByInvoiceAsync(userId, invoiceId, cancellationToken);
    }

    [HttpDelete("{invoiceId:guid}/{paymentId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> Delete(
        Guid invoiceId, Guid paymentId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(ApiResponse<PaymentResponse>.Create(null, false, "Invalid token", 401));
        return await paymentService.DeleteAsync(userId, invoiceId, paymentId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
