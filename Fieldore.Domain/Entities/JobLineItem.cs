namespace Fieldore.Domain.Entities;

public sealed class JobLineItem : AuditableEntity
{
    public Guid JobId { get; set; }
    public int SortOrder { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
