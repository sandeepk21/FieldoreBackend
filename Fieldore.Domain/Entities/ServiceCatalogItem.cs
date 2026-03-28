namespace Fieldore.Domain.Entities;

public sealed class ServiceCatalogItem : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public decimal? DefaultUnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
