using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.ServiceCatalog.Contracts;

public interface IServiceCatalogService
{
    Task<ApiResponse<ServiceCatalogItemResponse>> CreateAsync(
        Guid userId,
        CreateServiceCatalogItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<ServiceCatalogItemResponse>>> GetAllAsync(
        Guid userId,
        GetServiceCatalogRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ServiceCatalogItemResponse>> GetByIdAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ServiceCatalogItemResponse>> UpdateAsync(
        Guid userId,
        Guid itemId,
        UpdateServiceCatalogItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteServiceCatalogItemResponse>> DeleteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default);
}
