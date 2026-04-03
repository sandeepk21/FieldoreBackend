namespace Fieldore.Domain.Entities;

public sealed class StateProvince : AuditableEntity
{
    public Guid CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Country? Country { get; set; }
}
