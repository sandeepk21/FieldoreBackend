using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Workers.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WorkersController(IWorkerService workerService) : ControllerBase
{
    [HttpGet("getAll")]
    public async Task<ActionResult<ApiResponse<List<WorkerResponse>>>> GetAll(
        [FromQuery] GetWorkersRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<WorkerResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await workerService.GetAllAsync(userId, request, cancellationToken);
    }

    [HttpGet("getAssignable")]
    public async Task<ActionResult<ApiResponse<List<WorkerResponse>>>> GetAssignable(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<List<WorkerResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await workerService.GetAssignableAsync(userId, cancellationToken);
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<WorkerResponse>>> Create(
        [FromBody] CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<WorkerResponse>.Create(null, false, "Invalid token", 401));
        }

        return await workerService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPut("update/{workerId:guid}")]
    public async Task<ActionResult<ApiResponse<WorkerResponse>>> Update(
        Guid workerId,
        [FromBody] UpdateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<WorkerResponse>.Create(null, false, "Invalid token", 401));
        }

        return await workerService.UpdateAsync(userId, workerId, request, cancellationToken);
    }

    [HttpPut("deactivate/{workerId:guid}")]
    public async Task<ActionResult<ApiResponse<WorkerResponse>>> Deactivate(
        Guid workerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<WorkerResponse>.Create(null, false, "Invalid token", 401));
        }

        return await workerService.DeactivateAsync(userId, workerId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
