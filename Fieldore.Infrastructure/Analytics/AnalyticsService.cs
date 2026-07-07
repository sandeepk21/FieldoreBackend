using Fieldore.Application.Analytics.Contracts;
using Fieldore.Application.Auth.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Analytics;

public sealed class AnalyticsService(FieldoreDbContext dbContext) : IAnalyticsService
{
    private static readonly Dictionary<string, string> CategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fuel"]          = "Fuel",
        ["materials"]     = "Materials",
        ["labor"]         = "Labour",
        ["equipment"]     = "Equipment",
        ["subcontractor"] = "Subcontractor",
        ["other"]         = "Other",
    };

    public async Task<ApiResponse<AnalyticsSummaryResponse>> GetSummaryAsync(
        Guid userId, string period, CancellationToken ct = default)
    {
        var businessId = await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (businessId is null)
            return ApiResponse<AnalyticsSummaryResponse>.Create(null, false, "Business not found", 404);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (periodStart, prevStart, prevEnd, chartPoints) = GetPeriodBounds(period, today);

        // ── Revenue & Expenses for current period ────────────────────────────
        var totalRevenue = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= periodStart
                     && i.IssuedOn <= today)
            .SumAsync(i => i.TotalAmount, ct);

        var totalExpenses = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId
                     && e.ExpenseDate >= periodStart
                     && e.ExpenseDate <= today)
            .SumAsync(e => e.Amount, ct);

        // ── Previous period revenue (for % change) ───────────────────────────
        var prevRevenue = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= prevStart
                     && i.IssuedOn <= prevEnd)
            .SumAsync(i => i.TotalAmount, ct);

        var revenueChangePct = prevRevenue == 0
            ? 0
            : (int)Math.Round((double)(totalRevenue - prevRevenue) / (double)prevRevenue * 100);

        // ── Jobs ─────────────────────────────────────────────────────────────
        var todayStart = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var jobs = await dbContext.Jobs
            .AsNoTracking()
            .Where(j => j.BusinessId == businessId
                     && j.ScheduledStartAt >= todayStart
                     && j.ScheduledStartAt <= todayEnd)
            .Select(j => j.Status)
            .ToListAsync(ct);

        var completedJobs   = jobs.Count(s => s == JobStatuses.Completed);
        var scheduledJobs   = jobs.Count(s => s == JobStatuses.Scheduled);
        var inProgressJobs  = jobs.Count(s => s == JobStatuses.InProgress);
        var cancelledJobs   = jobs.Count(s => s == JobStatuses.Cancelled);
        var avgJobValue     = completedJobs > 0 ? Math.Round(totalRevenue / completedJobs, 2) : 0;

        // ── New customers in period ───────────────────────────────────────────
        var periodStartDt = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var newCustomers = await dbContext.Customers
            .AsNoTracking()
            .CountAsync(c => c.BusinessId == businessId && c.CreatedAt >= periodStartDt, ct);

        // ── Revenue chart (grouped) ───────────────────────────────────────────
        var revenueByDate = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= periodStart
                     && i.IssuedOn <= today)
            .GroupBy(i => i.IssuedOn)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.TotalAmount) })
            .ToListAsync(ct);

        var expensesByDate = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId
                     && e.ExpenseDate >= periodStart
                     && e.ExpenseDate <= today)
            .GroupBy(e => e.ExpenseDate)
            .Select(g => new { Date = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        var revenueChart = chartPoints.Select(p => new RevenueChartPoint(
            p.Label,
            revenueByDate.Where(r => r.Date >= p.From && r.Date <= p.To).Sum(r => r.Total),
            expensesByDate.Where(e => e.Date >= p.From && e.Date <= p.To).Sum(e => e.Total)
        )).ToList();

        // ── Expenses by category ──────────────────────────────────────────────
        var catGroups = await dbContext.Expenses
            .AsNoTracking()
            .Where(e => e.BusinessId == businessId
                     && e.ExpenseDate >= periodStart
                     && e.ExpenseDate <= today)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        var expensesByCategory = catGroups
            .OrderByDescending(c => c.Total)
            .Select(c => new ExpenseCategoryBreakdown(
                c.Category,
                CategoryLabels.GetValueOrDefault(c.Category, c.Category),
                c.Total,
                totalExpenses > 0 ? (int)Math.Round((double)c.Total / (double)totalExpenses * 100) : 0
            )).ToList();

        // ── Top customers ─────────────────────────────────────────────────────
        var topCustomers = await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId
                     && i.Status == InvoiceStatuses.Paid
                     && i.IssuedOn >= periodStart
                     && i.IssuedOn <= today)
            .GroupBy(i => i.CustomerNameSnapshot)
            .Select(g => new
            {
                Name = g.Key,
                Revenue = g.Sum(i => i.TotalAmount),
                JobCount = g.Count()
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(ct);

        var result = new AnalyticsSummaryResponse(
            totalRevenue,
            totalExpenses,
            totalRevenue - totalExpenses,
            revenueChangePct,
            jobs.Count,
            completedJobs,
            newCustomers,
            avgJobValue,
            scheduledJobs,
            inProgressJobs,
            cancelledJobs,
            revenueChart,
            expensesByCategory,
            topCustomers.Select(t => new TopCustomerItem(t.Name, t.Revenue, t.JobCount)).ToList()
        );

        return ApiResponse<AnalyticsSummaryResponse>.Create(result, true, "Success", 200);
    }

    // ── Period helpers ────────────────────────────────────────────────────────
    private record ChartPoint(string Label, DateOnly From, DateOnly To);

    private static (DateOnly periodStart, DateOnly prevStart, DateOnly prevEnd, List<ChartPoint> points)
        GetPeriodBounds(string period, DateOnly today)
    {
        return period switch
        {
            "7d" => Build7D(today),
            "90d" => Build90D(today),
            "year" => BuildYear(today),
            _ => Build30D(today),   // default: 30d
        };
    }

    private static (DateOnly, DateOnly, DateOnly, List<ChartPoint>) Build7D(DateOnly today)
    {
        var start = today.AddDays(-6);
        var points = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var d = today.AddDays(i - 6);
                return new ChartPoint(d.ToString("dd MMM"), d, d);
            }).ToList();
        return (start, today.AddDays(-13), today.AddDays(-7), points);
    }

    private static (DateOnly, DateOnly, DateOnly, List<ChartPoint>) Build30D(DateOnly today)
    {
        var start = today.AddDays(-29);
        var points = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var weekEnd = today.AddDays(-(i * 7));
                var weekStart = weekEnd.AddDays(-6);
                if (weekStart < start) weekStart = start;
                return new ChartPoint($"W{5 - i}", weekStart, weekEnd);
            })
            .Reverse()
            .ToList();
        return (start, today.AddDays(-59), today.AddDays(-30), points);
    }

    private static (DateOnly, DateOnly, DateOnly, List<ChartPoint>) Build90D(DateOnly today)
    {
        var start = today.AddDays(-89);
        var points = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var wEnd = today.AddDays(-(i * 15));
                var wStart = wEnd.AddDays(-14);
                if (wStart < start) wStart = start;
                return new ChartPoint($"W{6 - i}", wStart, wEnd);
            })
            .Reverse()
            .ToList();
        return (start, today.AddDays(-179), today.AddDays(-90), points);
    }

    private static (DateOnly, DateOnly, DateOnly, List<ChartPoint>) BuildYear(DateOnly today)
    {
        var start = new DateOnly(today.Year - 1, today.Month, 1).AddMonths(1);
        var points = Enumerable.Range(0, 12)
            .Select(i =>
            {
                var m = today.AddMonths(i - 11);
                var mStart = new DateOnly(m.Year, m.Month, 1);
                var mEnd = mStart.AddMonths(1).AddDays(-1);
                return new ChartPoint(mStart.ToString("MMM"), mStart, mEnd);
            }).ToList();
        return (start, start.AddYears(-1), start.AddDays(-1), points);
    }
}
