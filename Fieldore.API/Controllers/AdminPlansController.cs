using Fieldore.Application.Admin.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize(Policy = PlatformRoles.AdminPolicy)]
[Route("api/admin/plans")]
public sealed class AdminPlansController(IAdminPlanService service) : ControllerBase
{
    [HttpGet]
    public Task<ApiResponse<List<AdminPlanResponse>>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpGet("{id:guid}")]
    public Task<ApiResponse<AdminPlanResponse>> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public Task<ApiResponse<AdminPlanResponse>> Create([FromBody] CreatePlanRequest request, CancellationToken ct)
        => service.CreateAsync(request, ct);

    [HttpPut("{id:guid}")]
    public Task<ApiResponse<AdminPlanResponse>> Update(Guid id, [FromBody] UpdatePlanRequest request, CancellationToken ct)
        => service.UpdateAsync(id, request, ct);

    [HttpPut("{id:guid}/prices")]
    public Task<ApiResponse<AdminPlanResponse>> ReplacePrices(Guid id, [FromBody] ReplacePricesRequest request, CancellationToken ct)
        => service.ReplacePricesAsync(id, request, ct);

    [HttpPut("{id:guid}/features")]
    public Task<ApiResponse<AdminPlanResponse>> ReplaceFeatures(Guid id, [FromBody] ReplaceFeaturesRequest request, CancellationToken ct)
        => service.ReplaceFeaturesAsync(id, request, ct);

    [HttpPost("{id:guid}/state/{action}")]
    public Task<ApiResponse<AdminPlanResponse>> SetState(Guid id, string action, CancellationToken ct)
        => service.SetStateAsync(id, action, ct);

    [HttpPost("{id:guid}/duplicate")]
    public Task<ApiResponse<AdminPlanResponse>> Duplicate(Guid id, CancellationToken ct) => service.DuplicateAsync(id, ct);

    [HttpPut("reorder")]
    public Task<ApiResponse<bool>> Reorder([FromBody] ReorderPlansRequest request, CancellationToken ct)
        => service.ReorderAsync(request, ct);

    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<bool>> Delete(Guid id, CancellationToken ct) => service.DeleteAsync(id, ct);

    [HttpPost("{id:guid}/sync")]
    public Task<ApiResponse<AdminPlanResponse>> Sync(Guid id, CancellationToken ct) => service.SyncToStripeAsync(id, ct);
}
