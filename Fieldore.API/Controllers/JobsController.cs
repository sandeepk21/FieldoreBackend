using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class JobsController(IJobService jobService, IWebHostEnvironment environment) : ControllerBase
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

        var response = await jobService.GetByIdAsync(userId, jobId, cancellationToken);
        if (response.Data is null)
        {
            return response;
        }

        return ApiResponse<JobResponse>.Create(
            BuildJobResponseWithAbsolutePhotoUrls(response.Data),
            response.Success,
            response.Message,
            response.StatusCode);
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

    [HttpPut("replace-line-items/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> ReplaceLineItems(
        Guid jobId,
        [FromBody] ReplaceJobLineItemsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.ReplaceLineItemsAsync(userId, jobId, request, cancellationToken);
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

    [HttpPut("edit-note/{jobId:guid}/{noteId:guid}")]
    public async Task<ActionResult<ApiResponse<JobNoteResponse>>> EditNote(
        Guid jobId,
        Guid noteId,
        [FromBody] UpdateJobNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobNoteResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.UpdateNoteAsync(userId, jobId, noteId, request, cancellationToken);
    }

    [HttpDelete("delete-note/{jobId:guid}/{noteId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteJobNoteResponse>>> DeleteNote(
        Guid jobId,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteJobNoteResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.DeleteNoteAsync(userId, jobId, noteId, cancellationToken);
    }

    [HttpPost("add-photo/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobPhotoResponse>>> AddPhoto(
        Guid jobId,
        [FromForm] AddJobPhotoFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobPhotoResponse>.Create(null, false, "Invalid token", 401));
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<JobPhotoResponse>.Create(null, false, "Photo file is required", 400));
        }

        var uploadsRoot = Path.Combine(environment.ContentRootPath, "uploads", "jobs", jobId.ToString("N"));
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(request.File.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        var photoRequest = new AddJobPhotoRequest(
            Path.Combine("uploads", "jobs", jobId.ToString("N"), fileName).Replace("\\", "/"),
            request.Caption,
            request.TakenAt);

        var response = await jobService.AddPhotoAsync(userId, jobId, photoRequest, cancellationToken);
        if (response.Data is null)
        {
            return response;
        }

        return ApiResponse<JobPhotoResponse>.Create(
            BuildPhotoResponseWithAbsoluteUrl(response.Data),
            response.Success,
            response.Message,
            response.StatusCode);
    }

    [HttpDelete("delete-photo/{jobId:guid}/{photoId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteJobPhotoResponse>>> DeletePhoto(
        Guid jobId,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteJobPhotoResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.DeletePhotoAsync(userId, jobId, photoId, cancellationToken);
    }

    [HttpPut("reorder-checklist/{jobId:guid}")]
    public async Task<ActionResult<ApiResponse<JobResponse>>> ReorderChecklist(
        Guid jobId,
        [FromBody] ReorderJobChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<JobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await jobService.ReorderChecklistAsync(userId, jobId, request, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }

    private JobResponse BuildJobResponseWithAbsolutePhotoUrls(JobResponse jobResponse)
    {
        var photos = jobResponse.Photos
            .Select(BuildPhotoResponseWithAbsoluteUrl)
            .ToList();

        return jobResponse with { Photos = photos };
    }

    private JobPhotoResponse BuildPhotoResponseWithAbsoluteUrl(JobPhotoResponse photoResponse)
    {
        return photoResponse with { StoragePath = BuildAbsoluteUrl(photoResponse.StoragePath) };
    }

    private string BuildAbsoluteUrl(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out _))
        {
            return path;
        }

        var normalizedPath = path.TrimStart('/');
        return $"{Request.Scheme}://{Request.Host}/{normalizedPath}";
    }
}

public sealed class AddJobPhotoFormRequest
{
    public IFormFile? File { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset? TakenAt { get; set; }
}
