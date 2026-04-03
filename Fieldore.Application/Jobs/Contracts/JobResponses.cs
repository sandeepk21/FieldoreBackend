namespace Fieldore.Application.Jobs.Contracts;

public sealed record JobAddressResponse(
    string? Line1,
    string? Line2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record JobCustomerSummaryResponse(
    Guid Id,
    string DisplayName,
    string MobilePhone,
    string? Email);

public sealed record JobAssignmentResponse(
    Guid Id,
    Guid UserProfileId,
    bool IsPrimary,
    string DisplayName,
    string? Email);

public sealed record JobChecklistItemResponse(
    Guid Id,
    int SortOrder,
    string TaskName,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByUserId);

public sealed record JobNoteResponse(
    Guid Id,
    Guid? CreatedByUserId,
    string Body,
    DateTimeOffset CreatedAt,
    string? CreatedByDisplayName);

public sealed record JobPhotoResponse(
    Guid Id,
    Guid? UploadedByUserId,
    string StoragePath,
    string? Caption,
    DateTimeOffset? TakenAt,
    DateTimeOffset CreatedAt);

public sealed record JobResponse(
    Guid Id,
    Guid BusinessId,
    Guid CustomerId,
    Guid? SourceLeadId,
    string JobNumber,
    string Title,
    string? JobType,
    string Priority,
    string Status,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    DateTimeOffset? ActualStartAt,
    DateTimeOffset? ActualEndAt,
    int? EstimatedDurationMinutes,
    bool UseCustomerPrimaryAddress,
    JobAddressResponse? ServiceAddress,
    string? Description,
    JobCustomerSummaryResponse? Customer,
    List<JobAssignmentResponse> Assignments,
    List<JobChecklistItemResponse> ChecklistItems,
    List<JobNoteResponse> Notes,
    List<JobPhotoResponse> Photos,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DeleteJobResponse(Guid JobId, string Message);
