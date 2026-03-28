namespace Fieldore.Domain.Entities;

public sealed class JobNote : AuditableEntity
{
    public Guid JobId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}
