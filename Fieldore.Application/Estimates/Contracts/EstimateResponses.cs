namespace Fieldore.Application.Estimates.Contracts;

public sealed record EstimateAddressResponse(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record EstimateCustomerSummaryResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    string MobilePhone);

public sealed record EstimateLineItemResponse(
    Guid Id,
    int SortOrder,
    string ServiceName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record EstimateAttachmentResponse(
    Guid Id,
    string FileName,
    string StoragePath,
    string? ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAt);

public sealed record EstimateResponse(
    Guid Id,
    Guid BusinessId,
    Guid CustomerId,
    string EstimateNumber,
    string Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    decimal TaxRate,
    decimal DiscountAmount,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    string CustomerNameSnapshot,
    string? CustomerEmailSnapshot,
    EstimateAddressResponse? BillingAddress,
    EstimateCustomerSummaryResponse? Customer,
    List<EstimateLineItemResponse> LineItems,
    Guid? PublicToken,
    DateTimeOffset? SentAt,
    DateTimeOffset? RespondedAt,
    Guid? ConvertedJobId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Title = null,
    string? InternalNotes = null,
    string DepositType = "none",
    decimal DepositValue = 0m,
    decimal DepositAmount = 0m,
    List<EstimateAttachmentResponse>? Attachments = null);

public sealed record DeleteEstimateResponse(Guid EstimateId, string Message);

public sealed record DeleteEstimateAttachmentResponse(Guid AttachmentId, string Message);

public sealed record ConvertEstimateToJobResponse(Guid EstimateId, Guid JobId, string Message);

// Anonymous, client-facing view of a quote (no internal identifiers leaked beyond what's shown).
public sealed record PublicEstimateResponse(
    Guid Id,
    string EstimateNumber,
    string Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    string BusinessName,
    string CustomerNameSnapshot,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    List<EstimateLineItemResponse> LineItems,
    bool CanRespond,
    string? Title = null,
    decimal DepositAmount = 0m);
