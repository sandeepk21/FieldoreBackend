using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Jobs.Contracts;

public interface IJobService
{
    Task<ApiResponse<JobResponse>> CreateAsync(
        Guid userId,
        CreateJobRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<JobResponse>>> GetAllAsync(
        Guid userId,
        GetJobsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> GetByIdAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> UpdateAsync(
        Guid userId,
        Guid jobId,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteJobResponse>> DeleteAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> UpdateStatusAsync(
        Guid userId,
        Guid jobId,
        UpdateJobStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> ReplaceAssignmentsAsync(
        Guid userId,
        Guid jobId,
        ReplaceJobAssignmentsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> ReplaceChecklistAsync(
        Guid userId,
        Guid jobId,
        ReplaceJobChecklistRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobNoteResponse>> AddNoteAsync(
        Guid userId,
        Guid jobId,
        AddJobNoteRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobNoteResponse>> UpdateNoteAsync(
        Guid userId,
        Guid jobId,
        Guid noteId,
        UpdateJobNoteRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteJobNoteResponse>> DeleteNoteAsync(
        Guid userId,
        Guid jobId,
        Guid noteId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobPhotoResponse>> AddPhotoAsync(
        Guid userId,
        Guid jobId,
        AddJobPhotoRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteJobPhotoResponse>> DeletePhotoAsync(
        Guid userId,
        Guid jobId,
        Guid photoId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<JobResponse>> ReorderChecklistAsync(
        Guid userId,
        Guid jobId,
        ReorderJobChecklistRequest request,
        CancellationToken cancellationToken = default);
}
