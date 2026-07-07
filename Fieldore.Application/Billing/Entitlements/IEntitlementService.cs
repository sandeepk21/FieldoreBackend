namespace Fieldore.Application.Billing.Entitlements;

public interface IEntitlementService
{
    /// <summary>Resolve the current plan + features + usage for a business.</summary>
    Task<EntitlementSet> GetForBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);
}
