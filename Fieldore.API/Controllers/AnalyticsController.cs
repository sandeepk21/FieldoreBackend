using System.Security.Claims;
using Fieldore.Application.Analytics.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string period = "30d",
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        return Ok(await analyticsService.GetSummaryAsync(userId, period, cancellationToken));
    }
}
