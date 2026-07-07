using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Dashboard.Contracts;

public record DashboardJobItem(
    Guid Id,
    string JobNumber,
    string CustomerName,
    string ScheduledAt,
    string? JobType,
    string Status
);

public record DashboardInvoiceItem(
    Guid Id,
    string InvoiceNumber,
    string CustomerName,
    decimal TotalAmount,
    decimal BalanceDue,
    string Status
);

public record DashboardSummaryResponse(
    string BusinessName,
    string Currency,
    int TodayJobsCount,
    int TodayJobsCompleted,
    int TodayJobsInProgress,
    int TodayJobsScheduled,
    decimal ThisMonthRevenue,
    decimal LastMonthRevenue,
    int RevenueChangePercent,
    decimal OutstandingAmount,
    int OverdueInvoicesCount,
    decimal NetProfitThisMonth,
    int ActiveJobsCount,
    List<DashboardJobItem> TodayJobs,
    List<DashboardInvoiceItem> RecentInvoices,
    List<decimal> WeeklyRevenue
);

public interface IDashboardService
{
    Task<ApiResponse<DashboardSummaryResponse>> GetSummaryAsync(Guid userId, CancellationToken ct = default);
}
