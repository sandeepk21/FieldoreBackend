using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class PaymentRecord : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = PaymentMethods.Other;
    public DateTimeOffset PaidAt { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public bool IsRefund { get; set; } = false;
    public Guid? RefundedPaymentId { get; set; }
}
