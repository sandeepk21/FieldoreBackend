namespace Fieldore.Application.Auth.Contracts;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Fieldore.API";
    public string Audience { get; set; } = "Fieldore.Client";
    public string SecretKey { get; set; } = "fieldore-dev-secret-key-change-me-123456";
    public int ExpirationMinutes { get; set; } = 120;
}
