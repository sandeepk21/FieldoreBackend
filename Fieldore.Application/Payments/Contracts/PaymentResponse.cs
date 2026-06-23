namespace Fieldore.Application.Payments.Contracts;

public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt,
    string? ReferenceNumber,
    string? Notes,
    bool IsStripePayment,
    DateTimeOffset CreatedAt);
