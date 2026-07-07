namespace Fieldore.Domain.Entities;

public sealed class AuthUser : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Fieldore staff who can access the admin panel (manage plans, subscriptions, analytics).</summary>
    public bool IsPlatformAdmin { get; set; }
}
