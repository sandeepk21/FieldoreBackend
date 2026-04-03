using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Customers.Contracts;

public interface ICustomerService
{
    Task<ApiResponse<CustomerResponse>> CreateAsync(
        Guid userId,
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<CustomerResponse>>> GetAllAsync(
        Guid userId,
        GetCustomersRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CustomerResponse>> GetByIdAsync(
        Guid userId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CustomerResponse>> UpdateAsync(
        Guid userId,
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteCustomerResponse>> DeleteAsync(
        Guid userId,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
