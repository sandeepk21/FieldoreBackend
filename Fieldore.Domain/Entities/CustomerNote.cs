namespace Fieldore.Domain.Entities;

public sealed class CustomerNote : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}
