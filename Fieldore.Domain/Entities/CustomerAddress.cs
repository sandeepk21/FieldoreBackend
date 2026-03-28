using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class CustomerAddress : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public string Label { get; set; } = "Service";
    public bool IsPrimary { get; set; }
    public bool IsBilling { get; set; }
    public Address Address { get; set; } = new();
}
