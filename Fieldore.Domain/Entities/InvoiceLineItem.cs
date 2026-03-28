namespace Fieldore.Domain.Entities;

public sealed class InvoiceLineItem : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public int SortOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal LineTotal { get; set; }
}
