namespace Fieldore.Application.Workers.Contracts;

public sealed record WorkerResponse(
    Guid Id,
    Guid? MembershipId,
    string DisplayName,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);
