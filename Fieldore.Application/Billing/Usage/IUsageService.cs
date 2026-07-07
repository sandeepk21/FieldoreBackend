namespace Fieldore.Application.Billing.Usage;

/// <summary>
/// Maintains per-business usage counters for the current billing period.
/// Period is derived from the subscription's current period, falling back to the
/// calendar month when there is no active subscription period.
/// </summary>
public interface IUsageService
{
    Task IncrementCompletedJobsAsync(Guid businessId, CancellationToken cancellationToken = default);
}
