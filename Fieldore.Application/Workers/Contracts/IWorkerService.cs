using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Workers.Contracts;

public interface IWorkerService
{
    Task<ApiResponse<List<WorkerResponse>>> GetAllAsync(Guid userId, GetWorkersRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<WorkerResponse>>> GetAssignableAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerResponse>> CreateAsync(Guid userId, CreateWorkerRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerResponse>> UpdateAsync(Guid userId, Guid workerId, UpdateWorkerRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WorkerResponse>> DeactivateAsync(Guid userId, Guid workerId, CancellationToken cancellationToken = default);
}
