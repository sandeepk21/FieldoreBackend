using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.ServiceCatalog.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ServiceCatalogController(IServiceCatalogService serviceCatalogService) : ControllerBase
{
    [HttpPost("create-item")]
    public async Task<ActionResult<ApiResponse<ServiceCatalogItemResponse>>> Create(
        [FromBody] CreateServiceCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Invalid token", 401));
        }

        return await serviceCatalogService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPost("getAll-items")]
    public async Task<ActionResult<ApiResponse<PagedResponse<ServiceCatalogItemResponse>>>> GetAll(
        [FromQuery] GetServiceCatalogRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PagedResponse<ServiceCatalogItemResponse>>.Create(null, false, "Invalid token", 401));
        }

        return await serviceCatalogService.GetAllAsync(userId, request, cancellationToken);
    }

    [HttpGet("getById/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceCatalogItemResponse>>> GetById(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Invalid token", 401));
        }

        return await serviceCatalogService.GetByIdAsync(userId, itemId, cancellationToken);
    }

    [HttpPut("update-item/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceCatalogItemResponse>>> Update(
        Guid itemId,
        [FromBody] UpdateServiceCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Invalid token", 401));
        }

        return await serviceCatalogService.UpdateAsync(userId, itemId, request, cancellationToken);
    }

    [HttpDelete("delete-item/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteServiceCatalogItemResponse>>> Delete(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteServiceCatalogItemResponse>.Create(null, false, "Invalid token", 401));
        }

        return await serviceCatalogService.DeleteAsync(userId, itemId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
