namespace Fieldore.Application.Payments.Contracts;

public sealed record RecordPaymentRequest(
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt,
    string? ReferenceNumber,
    string? Notes);
