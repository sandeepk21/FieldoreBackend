using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public string Type { get; set; } = CustomerTypes.Residential;
    public string? CompanyName { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MobilePhone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? GateCode { get; set; }
    public string? PetsNote { get; set; }
    public string? InternalNotes { get; set; }
    public bool BillingSameAsService { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<CustomerAddress> Addresses { get; set; } = [];
    public List<CustomerNote> Notes { get; set; } = [];
}
