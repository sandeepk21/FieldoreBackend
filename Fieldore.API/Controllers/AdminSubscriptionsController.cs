using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize(Policy = PlatformRoles.AdminPolicy)]
[Route("api/admin/subscriptions")]
public sealed class AdminSubscriptionsController(IAdminSubscriptionService service) : ControllerBase
{
    [HttpGet]
    public Task<ApiResponse<List<AdminSubscriptionResponse>>> List([FromQuery] string? status, CancellationToken ct)
        => service.ListAsync(status, ct);

    [HttpGet("{businessId:guid}")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Get(Guid businessId, CancellationToken ct)
        => service.GetAsync(businessId, ct);

    [HttpPost("{businessId:guid}/assign")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Assign(Guid businessId, [FromBody] AssignPlanRequest request, CancellationToken ct)
        => service.AssignAsync(businessId, request, ct);

    [HttpPost("{businessId:guid}/change-plan")]
    public Task<ApiResponse<AdminSubscriptionResponse>> ChangePlan(Guid businessId, [FromBody] AssignPlanRequest request, CancellationToken ct)
        => service.ChangePlanAsync(businessId, request, ct);

    [HttpPost("{businessId:guid}/cancel")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Cancel(Guid businessId, [FromBody] CancelSubscriptionRequest request, CancellationToken ct)
        => service.CancelAsync(businessId, request, ct);

    [HttpPost("{businessId:guid}/resume")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Resume(Guid businessId, CancellationToken ct)
        => service.ResumeAsync(businessId, ct);

    [HttpPost("{businessId:guid}/pause")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Pause(Guid businessId, CancellationToken ct)
        => service.PauseAsync(businessId, ct);

    [HttpPost("{businessId:guid}/extend")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Extend(Guid businessId, [FromBody] ExtendSubscriptionRequest request, CancellationToken ct)
        => service.ExtendAsync(businessId, request, ct);

    [HttpPost("{businessId:guid}/expire")]
    public Task<ApiResponse<AdminSubscriptionResponse>> Expire(Guid businessId, CancellationToken ct)
        => service.ExpireAsync(businessId, ct);
}
