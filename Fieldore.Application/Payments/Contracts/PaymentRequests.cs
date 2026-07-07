namespace Fieldore.Application.Payments.Contracts;

public sealed record RecordPaymentRequest(
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt,
    string? ReferenceNumber,
    string? Notes);

public sealed record RecordRefundRequest(
    Guid PaymentId,
    decimal Amount,
    string? Notes);
