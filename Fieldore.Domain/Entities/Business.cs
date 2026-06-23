using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class Business : AuditableEntity
{
    public Guid AuthUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeType { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string Currency { get; set; } = "USD";
    public Address? Address { get; set; }
    public List<BusinessMembership> Memberships { get; set; } = [];
    public List<ServiceCatalogItem> Services { get; set; } = [];
    public string? StripeAccountId { get; set; }
    public bool StripeOnboardingComplete { get; set; }
}
