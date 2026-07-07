using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Admin.Contracts;

public sealed record PlanDistributionDto(string PlanName, int Count);

public sealed record AdminDashboardResponse(
    int TotalSubscribers,
    int ActiveSubscribers,
    int TrialUsers,
    int PastDue,
    int Cancelled,
    int Expired,
    decimal Mrr,
    decimal Arr,
    decimal MonthlyRevenue,
    decimal HalfYearlyRevenue,
    int RenewalsThisMonth,
    List<PlanDistributionDto> PlanDistribution);

public interface IAdminAnalyticsService
{
    Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync(CancellationToken ct = default);
}
