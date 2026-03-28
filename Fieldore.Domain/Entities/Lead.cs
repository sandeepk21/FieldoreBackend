using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class Lead : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string RequestedService { get; set; } = string.Empty;
    public string Source { get; set; } = LeadSources.Other;
    public string Status { get; set; } = LeadStatuses.New;
    public string? Notes { get; set; }
    public DateTimeOffset? ContactedAt { get; set; }
    public DateTimeOffset? ConvertedAt { get; set; }
}
