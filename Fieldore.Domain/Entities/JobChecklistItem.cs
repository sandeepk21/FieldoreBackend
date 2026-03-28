namespace Fieldore.Domain.Entities;

public sealed class JobChecklistItem : AuditableEntity
{
    public Guid JobId { get; set; }
    public int SortOrder { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
}
