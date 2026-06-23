using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Expenses.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Expenses;

public sealed class ExpenseService(FieldoreDbContext dbContext) : IExpenseService
{
    private static readonly Dictionary<string, string> CategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        [ExpenseCategories.Fuel]          = "Fuel",
        [ExpenseCategories.Materials]     = "Materials",
        [ExpenseCategories.Labor]         = "Labour",
        [ExpenseCategories.Equipment]     = "Equipment",
        [ExpenseCategories.Subcontractor] = "Subcontractor",
        [ExpenseCategories.Other]         = "Other",
    };

    public async Task<ApiResponse<List<ExpenseResponse>>> GetAllAsync(
        Guid userId, GetExpensesRequest request, CancellationToken ct = default)
    {
        var businessId = await GetBusinessIdAsync(userId, ct);
        if (businessId is null)
            return ApiResponse<List<ExpenseResponse>>.Create(null, false, "Business not found", 404);

        var query = dbContext.Expenses
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value);

        if (request.JobId.HasValue)
            query = query.Where(x => x.JobId == request.JobId.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(x => x.Category == request.Category.Trim().ToLowerInvariant());

        if (request.DateFrom.HasValue)
            query = query.Where(x => x.ExpenseDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(x => x.ExpenseDate <= request.DateTo.Value);

        var expenses = await query
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return ApiResponse<List<ExpenseResponse>>.Create(
            expenses.Select(MapToResponse).ToList(), true, "Expenses retrieved", 200);
    }

    public async Task<ApiResponse<ExpenseSummaryResponse>> GetSummaryAsync(
        Guid userId, CancellationToken ct = default)
    {
        var businessId = await GetBusinessIdAsync(userId, ct);
        if (businessId is null)
            return ApiResponse<ExpenseSummaryResponse>.Create(null, false, "Business not found", 404);

        var expenses = await dbContext.Expenses
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value)
            .ToListAsync(ct);

        var totalExpenses = expenses.Sum(x => x.Amount);

        var totalRevenue = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value && x.Status == InvoiceStatuses.Paid)
            .SumAsync(x => x.TotalAmount, ct);

        var byCategory = expenses
            .GroupBy(x => x.Category)
            .Select(g => new CategoryBreakdown(
                g.Key,
                CategoryLabels.GetValueOrDefault(g.Key, g.Key),
                g.Sum(x => x.Amount),
                g.Count()))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return ApiResponse<ExpenseSummaryResponse>.Create(
            new ExpenseSummaryResponse(totalExpenses, totalRevenue, totalRevenue - totalExpenses, byCategory),
            true, "Summary retrieved", 200);
    }

    public async Task<ApiResponse<ExpenseResponse>> CreateAsync(
        Guid userId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        var businessId = await GetBusinessIdAsync(userId, ct);
        if (businessId is null)
            return ApiResponse<ExpenseResponse>.Create(null, false, "Business not found", 404);

        var validation = Validate(request.Amount, request.Category, request.Description);
        if (validation is not null)
            return ApiResponse<ExpenseResponse>.Create(null, false, validation, 400);

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            JobId = request.JobId,
            InvoiceId = request.InvoiceId,
            Category = request.Category.Trim().ToLowerInvariant(),
            Description = request.Description.Trim(),
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            ExpenseDate = request.ExpenseDate,
            VendorName = string.IsNullOrWhiteSpace(request.VendorName) ? null : request.VendorName.Trim(),
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(ct);

        return ApiResponse<ExpenseResponse>.Create(MapToResponse(expense), true, "Expense created", 201);
    }

    public async Task<ApiResponse<ExpenseResponse>> UpdateAsync(
        Guid userId, Guid expenseId, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        var businessId = await GetBusinessIdAsync(userId, ct);
        if (businessId is null)
            return ApiResponse<ExpenseResponse>.Create(null, false, "Business not found", 404);

        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(x => x.Id == expenseId && x.BusinessId == businessId.Value, ct);
        if (expense is null)
            return ApiResponse<ExpenseResponse>.Create(null, false, "Expense not found", 404);

        var validation = Validate(request.Amount, request.Category, request.Description);
        if (validation is not null)
            return ApiResponse<ExpenseResponse>.Create(null, false, validation, 400);

        expense.Category = request.Category.Trim().ToLowerInvariant();
        expense.Description = request.Description.Trim();
        expense.Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        expense.ExpenseDate = request.ExpenseDate;
        expense.JobId = request.JobId;
        expense.InvoiceId = request.InvoiceId;
        expense.VendorName = string.IsNullOrWhiteSpace(request.VendorName) ? null : request.VendorName.Trim();
        expense.ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim();
        expense.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await dbContext.SaveChangesAsync(ct);

        return ApiResponse<ExpenseResponse>.Create(MapToResponse(expense), true, "Expense updated", 200);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        Guid userId, Guid expenseId, CancellationToken ct = default)
    {
        var businessId = await GetBusinessIdAsync(userId, ct);
        if (businessId is null)
            return ApiResponse<bool>.Create(false, false, "Business not found", 404);

        var expense = await dbContext.Expenses
            .FirstOrDefaultAsync(x => x.Id == expenseId && x.BusinessId == businessId.Value, ct);
        if (expense is null)
            return ApiResponse<bool>.Create(false, false, "Expense not found", 404);

        dbContext.Expenses.Remove(expense);
        await dbContext.SaveChangesAsync(ct);

        return ApiResponse<bool>.Create(true, true, "Expense deleted", 200);
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken ct) =>
        await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

    private static ExpenseResponse MapToResponse(Expense e) =>
        new(e.Id, e.BusinessId, e.JobId, e.InvoiceId, e.Category, e.Description,
            e.Amount, e.ExpenseDate, e.VendorName, e.ReferenceNumber, e.Notes,
            e.CreatedAt, e.UpdatedAt);

    private static string? Validate(decimal amount, string category, string description)
    {
        if (amount <= 0) return "Amount must be greater than zero";
        if (string.IsNullOrWhiteSpace(description)) return "Description is required";
        if (string.IsNullOrWhiteSpace(category)) return "Category is required";
        if (!ExpenseCategories.All.Contains(category.Trim().ToLowerInvariant()))
            return $"Category must be one of: {string.Join(", ", ExpenseCategories.All)}";
        return null;
    }
}
