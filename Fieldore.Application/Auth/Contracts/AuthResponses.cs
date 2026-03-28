namespace Fieldore.Application.Auth.Contracts;

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    Guid? BusinessId);

public sealed record ForgotPasswordResponse(string Message);
