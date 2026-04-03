namespace Fieldore.Domain.Entities;

public sealed class Country : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ICollection<StateProvince> States { get; set; } = new List<StateProvince>();
}
