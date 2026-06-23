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

    // Optional upfront deposit requested to confirm the quote.
    public string DepositType { get; set; } = EstimateDepositTypes.None; // none | percent | fixed
    public decimal DepositValue { get; set; }   // raw % or fixed amount entered by the provider
    public decimal DepositAmount { get; set; }  // resolved currency amount (computed)

    public string? Title { get; set; }           // optional headline, e.g. "Kitchen remodel"
    public string? Notes { get; set; }           // shown to the client on the public quote
    public string? InternalNotes { get; set; }   // private to the business; never shown to the client
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string? CustomerEmailSnapshot { get; set; }
    public Address? BillingAddressSnapshot { get; set; }

    // Public-link workflow: client views/accepts/rejects the quote via a tokenized page.
    public Guid? PublicToken { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Guid? ConvertedJobId { get; set; }

    public List<EstimateLineItem> LineItems { get; set; } = [];
}
