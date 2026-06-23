namespace Fieldore.Application.ServiceCatalog.Contracts;

public sealed record CreateServiceCatalogItemRequest(
    string Name,
    string? Category,
    string? Description,
    decimal? DefaultUnitPrice);

public sealed record UpdateServiceCatalogItemRequest(
    string Name,
    string? Category,
    string? Description,
    decimal? DefaultUnitPrice,
    bool IsActive);

public sealed class GetServiceCatalogRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public bool? IsActive { get; set; }
}
