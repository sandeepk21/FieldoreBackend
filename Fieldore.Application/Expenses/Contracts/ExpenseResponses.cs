namespace Fieldore.Application.Expenses.Contracts;

public sealed record ExpenseResponse(
    Guid Id,
    Guid BusinessId,
    Guid? JobId,
    Guid? InvoiceId,
    string Category,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    string? VendorName,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CategoryBreakdown(
    string Category,
    string Label,
    decimal Amount,
    int Count);

public sealed record ExpenseSummaryResponse(
    decimal TotalExpenses,
    decimal TotalRevenue,
    decimal NetProfit,
    List<CategoryBreakdown> ByCategory);
