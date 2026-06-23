using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Estimates.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

// Anonymous, token-secured endpoints consumed by client-facing pages (no JWT).
[ApiController]
[AllowAnonymous]
[Route("api/public")]
public sealed class PublicController(IEstimateService estimateService) : ControllerBase
{
    [HttpGet("quotes/{token:guid}")]
    public async Task<ActionResult<ApiResponse<PublicEstimateResponse>>> GetQuote(
        Guid token,
        CancellationToken cancellationToken)
    {
        return await estimateService.GetPublicByTokenAsync(token, cancellationToken);
    }

    [HttpPost("quotes/{token:guid}/accept")]
    public async Task<ActionResult<ApiResponse<PublicEstimateResponse>>> AcceptQuote(
        Guid token,
        CancellationToken cancellationToken)
    {
        return await estimateService.RespondPublicAsync(token, true, cancellationToken);
    }

    [HttpPost("quotes/{token:guid}/reject")]
    public async Task<ActionResult<ApiResponse<PublicEstimateResponse>>> RejectQuote(
        Guid token,
        CancellationToken cancellationToken)
    {
        return await estimateService.RespondPublicAsync(token, false, cancellationToken);
    }
}
