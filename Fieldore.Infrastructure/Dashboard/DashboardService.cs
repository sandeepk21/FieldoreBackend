using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Dashboard.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Dashboard;

public sealed class DashboardService(FieldoreDbContext dbContext) : IDashboardService
{
    public async Task<ApiResponse<DashboardSummaryResponse>> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthUserId == userId, ct);

        if (business is null)
            return ApiResponse<DashboardSummaryResponse>.Create(null, false, "Business not found", 404);

        var businessId = business.Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var weekStart = today.AddDays(-6);

        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Today's jobs joined with customers
        var todayJobs = await (
            from j in dbContext.Jobs
            join c in dbContext.Customers on j.CustomerId equals c.Id
            where j.BusinessId == businessId
                && j.ScheduledStartAt >= todayStart
                && j.ScheduledStartAt <= todayEnd
            orderby j.ScheduledStartAt
            select new DashboardJobItem(
                j.Id,
                j.JobNumber,
                (c.FirstName + " " + c.LastName).Trim(),
                j.ScheduledStartAt.ToLocalTime().ToString("hh:mm tt"),
                j.JobType,
                j.Status
            )
        ).AsNoTracking().ToListAsync(ct);

        // This month paid revenue
        var thisMonthRevenue = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= monthStart
                     && i.IssuedOn <= today)
            .SumAsync(i => i.TotalAmount, ct);

        // Last month paid revenue (for % change)
        var lastMonthStart = monthStart.AddMonths(-1);
        var lastMonthEnd = monthStart.AddDays(-1);
        var lastMonthRevenue = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= lastMonthStart
                     && i.IssuedOn <= lastMonthEnd)
            .SumAsync(i => i.TotalAmount, ct);

        var revenueChangePercent = lastMonthRevenue == 0
            ? 0
            : (int)Math.Round((double)(thisMonthRevenue - lastMonthRevenue) / (double)lastMonthRevenue * 100);

        // Outstanding balance (all unpaid non-void invoices)
        var outstandingAmount = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status != InvoiceStatuses.Paid
                     && i.Status != InvoiceStatuses.Void)
            .SumAsync(i => i.BalanceDueAmount, ct);

        // Overdue invoice count
        var overdueCount = await dbContext.Invoices
            .AsNoTracking()
            .CountAsync(i => i.BusinessId == businessId && i.Status == InvoiceStatuses.Overdue, ct);

        // This month expenses
        var thisMonthExpenses = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId
                     && e.ExpenseDate >= monthStart
                     && e.ExpenseDate <= today)
            .SumAsync(e => e.Amount, ct);

        // Active jobs count
        var activeJobsCount = await dbContext.Jobs
            .AsNoTracking()
            .CountAsync(j => j.BusinessId == businessId && j.Status == JobStatuses.InProgress, ct);

        // Recent invoices (last 5)
        var recentInvoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new DashboardInvoiceItem(
                i.Id,
                i.InvoiceNumber,
                i.CustomerNameSnapshot,
                i.TotalAmount,
                i.BalanceDueAmount,
                i.Status
            ))
            .ToListAsync(ct);

        // Weekly revenue — last 7 days grouped
        var weeklyGroups = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= weekStart
                     && i.IssuedOn <= today)
            .GroupBy(i => i.IssuedOn)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.TotalAmount) })
            .ToListAsync(ct);

        var weeklyRevenue = Enumerable.Range(0, 7)
            .Select(i => weeklyGroups.FirstOrDefault(g => g.Date == today.AddDays(i - 6))?.Total ?? 0m)
            .ToList();

        var summary = new DashboardSummaryResponse(
            business.Name,
            business.Currency,
            todayJobs.Count,
            todayJobs.Count(j => j.Status == JobStatuses.Completed),
            todayJobs.Count(j => j.Status == JobStatuses.InProgress),
            todayJobs.Count(j => j.Status == JobStatuses.Scheduled),
            thisMonthRevenue,
            lastMonthRevenue,
            revenueChangePercent,
            outstandingAmount,
            overdueCount,
            thisMonthRevenue - thisMonthExpenses,
            activeJobsCount,
            todayJobs,
            recentInvoices,
            weeklyRevenue
        );

        return ApiResponse<DashboardSummaryResponse>.Create(summary, true, "Success", 200);
    }
}

 