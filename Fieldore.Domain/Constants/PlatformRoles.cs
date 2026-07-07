namespace Fieldore.Domain.Constants;

/// <summary>
/// Platform-level (Fieldore staff) authorization — distinct from per-business
/// <see cref="BusinessMembershipRoles"/>. A platform admin manages plans, features,
/// subscriptions and analytics across all businesses.
/// </summary>
public static class PlatformRoles
{
    /// <summary>JWT claim added when <c>AuthUser.IsPlatformAdmin</c> is true.</summary>
    public const string IsPlatformAdminClaim = "is_platform_admin";

    /// <summary>Authorization policy name guarding <c>/api/admin/**</c>.</summary>
    public const string AdminPolicy = "PlatformAdmin";
}
