using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Estimates.Contracts;

public interface IEstimateService
{
    Task<ApiResponse<EstimateResponse>> CreateAsync(
        Guid userId,
        CreateEstimateRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<EstimateResponse>>> GetAllAsync(
        Guid userId,
        GetEstimatesRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<EstimateResponse>> GetByIdAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<EstimateResponse>> UpdateAsync(
        Guid userId,
        Guid estimateId,
        UpdateEstimateRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<EstimateResponse>> UpdateStatusAsync(
        Guid userId,
        Guid estimateId,
        UpdateEstimateStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteEstimateResponse>> DeleteAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default);

    // Attachments: the controller persists the uploaded file, then records it here.
    Task<ApiResponse<EstimateAttachmentResponse>> AddAttachmentAsync(
        Guid userId,
        Guid estimateId,
        AddEstimateAttachmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteEstimateAttachmentResponse>> DeleteAttachmentAsync(
        Guid userId,
        Guid estimateId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    // Marks the estimate as sent and (re)issues a public token for the client link.
    Task<ApiResponse<EstimateResponse>> SendAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default);

    // Creates a job from an approved estimate and marks it converted.
    Task<ApiResponse<ConvertEstimateToJobResponse>> ConvertToJobAsync(
        Guid userId,
        Guid estimateId,
        ConvertEstimateToJobRequest request,
        CancellationToken cancellationToken = default);

    // Anonymous, public-link operations (no business scoping; token is the secret).
    Task<ApiResponse<PublicEstimateResponse>> GetPublicByTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PublicEstimateResponse>> RespondPublicAsync(
        Guid token,
        bool accept,
        CancellationToken cancellationToken = default);
}
