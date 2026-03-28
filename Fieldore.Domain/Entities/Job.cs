using Fieldore.Domain.Constants;
using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class Job : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? SourceLeadId { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? JobType { get; set; }
    public string Priority { get; set; } = JobPriorities.Normal;
    public string Status { get; set; } = JobStatuses.Draft;
    public DateTimeOffset ScheduledStartAt { get; set; }
    public DateTimeOffset? ScheduledEndAt { get; set; }
    public DateTimeOffset? ActualStartAt { get; set; }
    public DateTimeOffset? ActualEndAt { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool UseCustomerPrimaryAddress { get; set; } = true;
    public Address? ServiceAddress { get; set; }
    public string? Description { get; set; }
    public List<JobAssignment> Assignments { get; set; } = [];
    public List<JobChecklistItem> ChecklistItems { get; set; } = [];
    public List<JobNote> Notes { get; set; } = [];
    public List<JobPhoto> Photos { get; set; } = [];
}
