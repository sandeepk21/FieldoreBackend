namespace Fieldore.Domain.Entities;

/// <summary>
/// Append-only log of processed Stripe Billing webhook events. The unique
/// <see cref="StripeEventId"/> gives idempotency (Stripe may deliver an event twice).
/// </summary>
public sealed class BillingEvent : AuditableEntity
{
    public Guid? BusinessId { get; set; }
    public string StripeEventId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    /// <summary>received | processed | failed | ignored</summary>
    public string Status { get; set; } = "received";
}
