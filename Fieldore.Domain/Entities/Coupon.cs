namespace Fieldore.Domain.Entities;

/// <summary>
/// Promotional discount code (future-proofing — schema shipped now, wiring later).
/// Backed by a Stripe Coupon/Promotion Code when synced.
/// </summary>
public sealed class Coupon : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal? PercentOff { get; set; }
    public decimal? AmountOff { get; set; }
    public string Currency { get; set; } = "USD";
    public int? MaxRedemptions { get; set; }
    public DateOnly? RedeemBy { get; set; }
    public string? StripeCouponId { get; set; }
    public bool IsActive { get; set; } = true;
}
