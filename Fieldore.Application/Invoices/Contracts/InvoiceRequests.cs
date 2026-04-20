namespace Fieldore.Application.Invoices.Contracts;

public sealed record InvoiceAddressRequest(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record InvoiceLineItemRequest(
    int SortOrder,
    string Name,
    string? Description,
    decimal Quantity,
    decimal UnitRate);

public sealed record CreateInvoiceRequest(
    Guid CustomerId,
    Guid? JobId,
    string? PurchaseOrderNumber,
    string? NetTerms,
    string Status,
    DateOnly IssuedOn,
    DateOnly DueOn,
    decimal TaxRate,
    decimal DiscountAmount,
    string? Notes,
    InvoiceAddressRequest? BillingAddress,
    List<InvoiceLineItemRequest>? LineItems);

public sealed record UpdateInvoiceRequest(
    Guid CustomerId,
    Guid? JobId,
    string? PurchaseOrderNumber,
    string? NetTerms,
    string Status,
    DateOnly IssuedOn,
    DateOnly DueOn,
    decimal TaxRate,
    decimal DiscountAmount,
    string? Notes,
    InvoiceAddressRequest? BillingAddress,
    List<InvoiceLineItemRequest>? LineItems);

public sealed record UpdateInvoiceStatusRequest(string Status);

public sealed class GetInvoicesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? JobId { get; set; }
    public string? Status { get; set; }
    public DateOnly? IssuedFrom { get; set; }
    public DateOnly? IssuedTo { get; set; }
    public DateOnly? DueFrom { get; set; }
    public DateOnly? DueTo { get; set; }
}
