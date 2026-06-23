namespace Fieldore.Domain.Entities;

public sealed class EstimateAttachment : AuditableEntity
{
    public Guid EstimateId { get; set; }
    public string FileName { get; set; } = string.Empty;   // original name, for display
    public string StoragePath { get; set; } = string.Empty; // relative path under the content root
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public Guid? UploadedByUserId { get; set; }
}
