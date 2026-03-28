namespace Fieldore.Domain.Entities;

public sealed class UserNotificationPreference : AuditableEntity
{
    public Guid UserProfileId { get; set; }
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool MarketingEnabled { get; set; }
}
