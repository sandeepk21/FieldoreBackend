namespace Fieldore.Application.ServiceCatalog.Contracts;

public sealed record ServiceCatalogItemResponse(
    Guid Id,
    string Name,
    string? Category,
    string? Description,
    decimal? DefaultUnitPrice,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DeleteServiceCatalogItemResponse(Guid Id, string Message);
