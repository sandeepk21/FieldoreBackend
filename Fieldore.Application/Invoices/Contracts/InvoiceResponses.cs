namespace Fieldore.Application.Invoices.Contracts;

public sealed record InvoiceAddressResponse(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record InvoiceCustomerSummaryResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    string MobilePhone);

public sealed record InvoiceLineItemResponse(
    Guid Id,
    int SortOrder,
    string Name,
    string? Description,
    decimal Quantity,
    decimal UnitRate,
    decimal LineTotal);

public sealed record PaymentRecordResponse(
    Guid Id,
    decimal Amount,
    string Method,
    DateTimeOffset PaidAt,
    string? ReferenceNumber,
    string? Notes,
    bool IsStripePayment,
    bool IsRefund,
    Guid? RefundedPaymentId,
    DateTimeOffset CreatedAt);

public sealed record InvoiceResponse(
    Guid Id,
    Guid BusinessId,
    Guid CustomerId,
    Guid? JobId,
    string InvoiceNumber,
    string? PurchaseOrderNumber,
    string NetTerms,
    string Status,
    DateOnly IssuedOn,
    DateOnly DueOn,
    decimal TaxRate,
    decimal DiscountAmount,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal BalanceDueAmount,
    string? Notes,
    string CustomerNameSnapshot,
    string? CustomerEmailSnapshot,
    InvoiceAddressResponse? BillingAddress,
    InvoiceCustomerSummaryResponse? Customer,
    Guid? PublicToken,
    List<InvoiceLineItemResponse> LineItems,
    List<PaymentRecordResponse> Payments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DeleteInvoiceResponse(Guid InvoiceId, string Message);

public sealed record SendInvoiceResponse(Guid InvoiceId, string InvoiceNumber, Guid PublicToken, string PublicUrl);
