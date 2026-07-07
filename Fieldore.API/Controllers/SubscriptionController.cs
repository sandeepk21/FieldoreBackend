using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Subscriptions.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>The signed-in provider's current plan, status, usage and feature states.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<MySubscriptionResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<MySubscriptionResponse>.Create(null, false, "Invalid token", 401));
        }

        return await subscriptionService.GetMineAsync(userId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
