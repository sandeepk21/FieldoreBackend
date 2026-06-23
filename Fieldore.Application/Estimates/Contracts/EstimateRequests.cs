namespace Fieldore.Application.Estimates.Contracts;

public sealed record EstimateAddressRequest(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record EstimateLineItemRequest(
    int SortOrder,
    string ServiceName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateEstimateRequest(
    Guid CustomerId,
    string Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    decimal TaxRate,
    decimal DiscountAmount,
    string? Notes,
    EstimateAddressRequest? BillingAddress,
    List<EstimateLineItemRequest>? LineItems,
    string? Title = null,
    string? InternalNotes = null,
    string? DepositType = null,
    decimal DepositValue = 0m);

public sealed record UpdateEstimateRequest(
    Guid CustomerId,
    string Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    decimal TaxRate,
    decimal DiscountAmount,
    string? Notes,
    EstimateAddressRequest? BillingAddress,
    List<EstimateLineItemRequest>? LineItems,
    string? Title = null,
    string? InternalNotes = null,
    string? DepositType = null,
    decimal DepositValue = 0m);

public sealed record UpdateEstimateStatusRequest(string Status, string? ConvertedJobId = null);

// Built by the controller after the uploaded file is persisted to disk.
public sealed record AddEstimateAttachmentRequest(
    string FileName,
    string StoragePath,
    string? ContentType,
    long FileSizeBytes,
    Guid? UploadedByUserId);

public sealed record ConvertEstimateToJobRequest(
    string? Title,
    DateTimeOffset? ScheduledStartAt);

public sealed class GetEstimatesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateOnly? IssuedFrom { get; set; }
    public DateOnly? IssuedTo { get; set; }
}
