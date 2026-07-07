using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Subscriptions.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fieldore.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public")]
[Route("api/[controller]")]
public sealed class PlansController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>Public plan catalog for the marketing pricing page (no auth).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PublicPlanResponse>>>> GetPlans(CancellationToken cancellationToken)
        => await subscriptionService.GetPublicPlansAsync(cancellationToken);
}
