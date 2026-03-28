using System.Security.Claims;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(request, cancellationToken);
    }

    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Signup([FromBody] SignupRequest request, CancellationToken cancellationToken)
    {
        return await authService.SignupAsync(request, cancellationToken);
    }

    [HttpPost("business-register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> BusinessRegister([FromBody] BusinessRegisterRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(ApiResponse<AuthResponse>.Create(
                null, false, "Invalid token", 401));
        }

        var userId = Guid.Parse(userIdClaim);

        return await authService.BusinessRegisterAsync(userId, request, cancellationToken);
    }
    [HttpGet("get-business-details")]
    public async Task<ActionResult<ApiResponse<BusinessDetailsResponse>>> GetBusiness(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(ApiResponse<BusinessDetailsResponse>.Create(
                null, false, "Invalid token", 401));
        }

        var userId = Guid.Parse(userIdClaim);

        return await authService.GetBusinessAsync(userId, cancellationToken);
    }
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResponse>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        return await authService.ForgotPasswordAsync(request, cancellationToken);
    }

    
}
