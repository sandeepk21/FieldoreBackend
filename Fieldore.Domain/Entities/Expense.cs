using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class Expense : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Category { get; set; } = ExpenseCategories.Other;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string? VendorName { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}
