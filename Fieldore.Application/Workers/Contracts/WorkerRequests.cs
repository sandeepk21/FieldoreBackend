namespace Fieldore.Application.Workers.Contracts;

public sealed record CreateWorkerRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role);

public sealed record UpdateWorkerRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Role);

public sealed class GetWorkersRequest
{
    public bool? IsActive { get; set; } = true;
    public string? Search { get; set; }
}
