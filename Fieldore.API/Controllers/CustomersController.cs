using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Customers.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpPost]
    [Route("create-customer")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<CustomerResponse>.Create(
                null, false, "Invalid token", 401));
        }

        return await customerService.CreateAsync(userId, request, cancellationToken);
    }

    [HttpPost]
    [Route("getAll-customers")]
    public async Task<ActionResult<ApiResponse<PagedResponse<CustomerResponse>>>> GetAll(
        GetCustomersRequest request,CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PagedResponse<CustomerResponse>>.Create(
                null, false, "Invalid token", 401));
        }

        return await customerService.GetAllAsync(userId, request,cancellationToken);
    }

    [HttpGet("getById/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetById(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<CustomerResponse>.Create(
                null, false, "Invalid token", 401));
        }

        return await customerService.GetByIdAsync(userId, customerId, cancellationToken);
    }

    [HttpPut("update-customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(
        Guid customerId,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<CustomerResponse>.Create(
                null, false, "Invalid token", 401));
        }

        return await customerService.UpdateAsync(userId, customerId, request, cancellationToken);
    }

    [HttpDelete("delete-customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<DeleteCustomerResponse>>> Delete(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse<DeleteCustomerResponse>.Create(
                null, false, "Invalid token", 401));
        }

        return await customerService.DeleteAsync(userId, customerId, cancellationToken);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out userId);
    }
}
