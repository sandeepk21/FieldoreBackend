using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class BusinessSubscription : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Provider { get; set; } = "sqlserver";
    public string? ProviderSubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = "monthly";
    public string Status { get; set; } = SubscriptionStatuses.Trial;
    public DateOnly? RenewsOn { get; set; }
    public DateOnly? TrialEndsOn { get; set; }
}
