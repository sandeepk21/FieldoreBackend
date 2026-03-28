namespace Fieldore.Application.Auth.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record SignupRequest(string Email, string Password, string FirstName, string LastName);

public sealed record BusinessRegisterRequest(
    string BusinessName,
    string TradeType,
    string Phone, 
    string AddressLine1,
    string? AddressLine2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country,
    string TimeZone
    );

public sealed record ForgotPasswordRequest(string Email);
