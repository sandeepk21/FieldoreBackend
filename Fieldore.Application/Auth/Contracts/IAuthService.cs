using Fieldore.Application.Models;

namespace Fieldore.Application.Auth.Contracts;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> BusinessRegisterAsync(Guid userId,BusinessRegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<BusinessDetailsResponse>> GetBusinessAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
}
