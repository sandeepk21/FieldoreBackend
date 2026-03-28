using Fieldore.Domain.Constants;
using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class Invoice : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? JobId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public string NetTerms { get; set; } = "Net 30";
    public string Status { get; set; } = InvoiceStatuses.Draft;
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueOn { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceDueAmount { get; set; }
    public string? Notes { get; set; }
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string? CustomerEmailSnapshot { get; set; }
    public Address? BillingAddressSnapshot { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = [];
    public List<PaymentRecord> Payments { get; set; } = [];
}
