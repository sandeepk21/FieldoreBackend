using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize(Policy = PlatformRoles.AdminPolicy)]
[Route("api/admin/analytics")]
public sealed class AdminAnalyticsController(IAdminAnalyticsService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<ApiResponse<AdminDashboardResponse>> Dashboard(CancellationToken ct) => service.GetDashboardAsync(ct);
}
