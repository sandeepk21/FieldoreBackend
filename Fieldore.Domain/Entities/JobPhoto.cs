namespace Fieldore.Domain.Entities;

public sealed class JobPhoto : AuditableEntity
{
    public Guid JobId { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTimeOffset? TakenAt { get; set; }
}
