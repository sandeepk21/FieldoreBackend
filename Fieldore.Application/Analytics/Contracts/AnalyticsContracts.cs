using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Analytics.Contracts;

public record RevenueChartPoint(string Label, decimal Revenue, decimal Expenses);

public record ExpenseCategoryBreakdown(string Category, string Label, decimal Amount, int Percent);

public record TopCustomerItem(string Name, decimal Revenue, int JobCount);

public record AnalyticsSummaryResponse(
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetProfit,
    int RevenueChangePercent,
    int TotalJobs,
    int CompletedJobs,
    int NewCustomers,
    decimal AvgJobValue,
    int ScheduledJobs,
    int InProgressJobs,
    int CancelledJobs,
    List<RevenueChartPoint> RevenueChart,
    List<ExpenseCategoryBreakdown> ExpensesByCategory,
    List<TopCustomerItem> TopCustomers
);

public interface IAnalyticsService
{
    Task<ApiResponse<AnalyticsSummaryResponse>> GetSummaryAsync(
        Guid userId,
        string period,
        CancellationToken ct = default);
}
