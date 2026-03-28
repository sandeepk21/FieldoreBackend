using Fieldore.Domain.ValueObjects;

namespace Fieldore.Domain.Entities;

public sealed class AppUserProfile : AuditableEntity
{
    public Guid AuthUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? TimeZone { get; set; }
    public bool IsActive { get; set; } = true;
}
