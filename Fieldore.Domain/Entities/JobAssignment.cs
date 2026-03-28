namespace Fieldore.Domain.Entities;

public sealed class JobAssignment : AuditableEntity
{
    public Guid JobId { get; set; }
    public Guid UserProfileId { get; set; }
    public bool IsPrimary { get; set; }
}
