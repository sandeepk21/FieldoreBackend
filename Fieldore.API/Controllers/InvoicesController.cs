using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Invoices.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpPost("create-invoice")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Create(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<InvoiceResponse>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPost("getAll-invoices")]
    public async Task<ActionResult<ApiResponse<PagedResponse<InvoiceResponse>>>> GetAll(
        [FromQuery] GetInvoicesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PagedResponse<InvoiceResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.GetAllAsync(userId, request, cancellationToken);
    }

    [HttpGet("getById/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetById(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<InvoiceResponse>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.GetByIdAsync(userId, invoiceId, cancellationToken);
    }

    [HttpPut("update-invoice/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Update(
        Guid invoiceId,
        [FromBody] UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<InvoiceResponse>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.UpdateAsync(userId, invoiceId, request, cancellationToken);
    }

    [HttpPatch("update-status/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateStatus(
        Guid invoiceId,
        [FromBody] UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<InvoiceResponse>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.UpdateStatusAsync(userId, invoiceId, request, cancellationToken);
    }

    [HttpDelete("delete-invoice/{invoiceId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteInvoiceResponse>>> Delete(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteInvoiceResponse>.Create(null, false, "Invalid token", 401));
        }

        return await invoiceService.DeleteAsync(userId, invoiceId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
