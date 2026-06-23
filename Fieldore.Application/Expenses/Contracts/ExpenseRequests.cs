namespace Fieldore.Application.Expenses.Contracts;

public sealed record CreateExpenseRequest(
    string Category,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    Guid? JobId,
    Guid? InvoiceId,
    string? VendorName,
    string? ReferenceNumber,
    string? Notes);

public sealed record UpdateExpenseRequest(
    string Category,
    string Description,
    decimal Amount,
    DateOnly ExpenseDate,
    Guid? JobId,
    Guid? InvoiceId,
    string? VendorName,
    string? ReferenceNumber,
    string? Notes);

public sealed class GetExpensesRequest
{
    public Guid? JobId { get; set; }
    public string? Category { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
