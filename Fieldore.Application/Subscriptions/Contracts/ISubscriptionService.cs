using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Subscriptions.Contracts;

public interface ISubscriptionService
{
    /// <summary>Public, dynamic plan catalog for the marketing pricing page.</summary>
    Task<ApiResponse<List<PublicPlanResponse>>> GetPublicPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>The signed-in provider's current plan, status, usage and feature states.</summary>
    Task<ApiResponse<MySubscriptionResponse>> GetMineAsync(Guid userId, CancellationToken cancellationToken = default);
}
