namespace Fieldore.Domain.Entities;

/// <summary>
/// Per-business usage counters for the current billing period. Rolled forward when a
/// new period starts (via Stripe webhook). Drives limit enforcement (e.g. job_limit).
/// </summary>
public sealed class SubscriptionUsage : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public int CompletedJobsCount { get; set; }
    public int InvoicesCreatedCount { get; set; }
    public int CustomersAddedCount { get; set; }
    public int EmployeesCount { get; set; }
    public long StorageUsedBytes { get; set; }
}
