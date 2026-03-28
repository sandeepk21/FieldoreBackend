using Fieldore.Domain.Constants;

namespace Fieldore.Domain.Entities;

public sealed class BusinessMembership : AuditableEntity
{
    public Guid BusinessId { get; set; }
    public Guid UserProfileId { get; set; }
    public string Role { get; set; } = BusinessMembershipRoles.Staff;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}
