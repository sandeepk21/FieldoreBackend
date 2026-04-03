using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    [HttpPost("create-job")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> Create(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPost("getAll-jobs")]
    public async Task<ActionResult<ApiResponse<PagedResponse<JobResponse>>>> GetAll(
        [FromQuery] GetJobsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PagedResponse<JobResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.GetAllAsync(userId, request, cancellationToken);
    }

    [HttpGet("getById/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> GetById(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.GetByIdAsync(userId, jobId, cancellationToken);
    }

    [HttpPut("update-job/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> Update(
        Guid jobId,
        [FromBody] UpdateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.UpdateAsync(userId, jobId, request, cancellationToken);
    }

    [HttpDelete("delete-job/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteJobResponse>>> Delete(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteJobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.DeleteAsync(userId, jobId, cancellationToken);
    }

    [HttpPatch("update-status/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> UpdateStatus(
        Guid jobId,
        [FromBody] UpdateJobStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.UpdateStatusAsync(userId, jobId, request, cancellationToken);
    }

    [HttpPut("replace-assignments/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> ReplaceAssignments(
        Guid jobId,
        [FromBody] ReplaceJobAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.ReplaceAssignmentsAsync(userId, jobId, request, cancellationToken);
    }

    [HttpPut("replace-checklist/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> ReplaceChecklist(
        Guid jobId,
        [FromBody] ReplaceJobChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.ReplaceChecklistAsync(userId, jobId, request, cancellationToken);
    }

    [HttpPost("add-note/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobNoteResponse>>> AddNote(
        Guid jobId,
        [FromBody] AddJobNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobNoteResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.AddNoteAsync(userId, jobId, request, cancellationToken);
    }

    [HttpPost("add-photo/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobPhotoResponse>>> AddPhoto(
        Guid jobId,
        [FromBody] AddJobPhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobPhotoResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.AddPhotoAsync(userId, jobId, request, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
