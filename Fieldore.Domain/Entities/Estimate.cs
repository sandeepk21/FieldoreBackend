using Fieldore.Domain.Constants;
using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class Estimate : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string Status { get; set; } = EstimateStatuses.Draft;
    public DateOnly IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string? CustomerEmailSnapshot { get; set; }
    public Address? BillingAddressSnapshot { get; set; }
    public List<EstimateLineItem> LineItems { get; set; } = [];
}
