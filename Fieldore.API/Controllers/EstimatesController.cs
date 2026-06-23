using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Estimates.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class EstimatesController(IEstimateService estimateService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("create-estimate")]
    public async Task<ActionResult<ApiResponse<EstimateResponse>>> Create(
        [FromBody] CreateEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPost("getAll-estimates")]
    public async Task<ActionResult<ApiResponse<PagedResponse<EstimateResponse>>>> GetAll(
        [FromQuery] GetEstimatesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PagedResponse<EstimateResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.GetAllAsync(userId, request, cancellationToken);
    }

    [HttpGet("getById/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<EstimateResponse>>> GetById(
        Guid estimateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.GetByIdAsync(userId, estimateId, cancellationToken);
    }

    [HttpPut("update-estimate/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<EstimateResponse>>> Update(
        Guid estimateId,
        [FromBody] UpdateEstimateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.UpdateAsync(userId, estimateId, request, cancellationToken);
    }

    [HttpPatch("update-status/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<EstimateResponse>>> UpdateStatus(
        Guid estimateId,
        [FromBody] UpdateEstimateStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.UpdateStatusAsync(userId, estimateId, request, cancellationToken);
    }

    [HttpPost("send/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<EstimateResponse>>> Send(
        Guid estimateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.SendAsync(userId, estimateId, cancellationToken);
    }

    [HttpPost("convert-to-job/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<ConvertEstimateToJobResponse>>> ConvertToJob(
        Guid estimateId,
        [FromBody] ConvertEstimateToJobRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<ConvertEstimateToJobResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.ConvertToJobAsync(userId, estimateId, request, cancellationToken);
    }

    [HttpDelete("delete-estimate/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteEstimateResponse>>> Delete(
        Guid estimateId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteEstimateResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.DeleteAsync(userId, estimateId, cancellationToken);
    }

    [HttpPost("add-attachment/{estimateId:guid}")]
    public async Task<ActionResult<ApiResponse<EstimateAttachmentResponse>>> AddAttachment(
        Guid estimateId,
        [FromForm] AddEstimateAttachmentFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<EstimateAttachmentResponse>.Create(null, false, "Invalid token", 401));
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<EstimateAttachmentResponse>.Create(null, false, "Attachment file is required", 400));
        }

        const long maxBytes = 15 * 1024 * 1024; // 15 MB
        if (request.File.Length > maxBytes)
        {
            return BadRequest(ApiResponse<EstimateAttachmentResponse>.Create(null, false, "Attachment must be 15 MB or smaller", 400));
        }

        var uploadsRoot = Path.Combine(environment.ContentRootPath, "uploads", "estimates", estimateId.ToString("N"));
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(request.File.FileName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, storedName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = Path
            .Combine("uploads", "estimates", estimateId.ToString("N"), storedName)
            .Replace("\\", "/");

        var attachmentRequest = new AddEstimateAttachmentRequest(
            request.File.FileName,
            relativePath,
            request.File.ContentType,
            request.File.Length,
            userId);

        var response = await estimateService.AddAttachmentAsync(userId, estimateId, attachmentRequest, cancellationToken);

        // Estimate not found / not owned → remove the orphaned file we just wrote.
        if (response.Data is null && System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return response;
    }

    [HttpDelete("delete-attachment/{estimateId:guid}/{attachmentId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteEstimateAttachmentResponse>>> DeleteAttachment(
        Guid estimateId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteEstimateAttachmentResponse>.Create(null, false, "Invalid token", 401));
        }

        return await estimateService.DeleteAttachmentAsync(userId, estimateId, attachmentId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}

public sealed class AddEstimateAttachmentFormRequest
{
    public IFormFile? File { get; set; }
}
