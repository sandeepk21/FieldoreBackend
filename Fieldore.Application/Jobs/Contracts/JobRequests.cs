namespace Fieldore.Application.Jobs.Contracts;

public sealed record JobAddressRequest(
    string Line1,
    string? Line2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country,
    decimal? Latitude,
    decimal? Longitude);

public sealed record JobAssignmentRequest(
    Guid UserProfileId,
    bool IsPrimary);

public sealed record JobChecklistItemRequest(
    int SortOrder,
    string TaskName,
    bool IsCompleted);

public sealed record CreateJobRequest(
    Guid CustomerId,
    Guid? SourceLeadId,
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
    JobAddressRequest? ServiceAddress,
    string? Description,
    List<JobAssignmentRequest>? Assignments,
    List<JobChecklistItemRequest>? ChecklistItems);

public sealed record UpdateJobRequest(
    Guid CustomerId,
    Guid? SourceLeadId,
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
    JobAddressRequest? ServiceAddress,
    string? Description,
    List<JobAssignmentRequest>? Assignments,
    List<JobChecklistItemRequest>? ChecklistItems);

public sealed record UpdateJobStatusRequest(
    string Status,
    DateTimeOffset? ActualStartAt,
    DateTimeOffset? ActualEndAt);

public sealed record ReplaceJobAssignmentsRequest(
    List<JobAssignmentRequest>? Assignments);

public sealed record ReplaceJobChecklistRequest(
    List<JobChecklistItemRequest>? ChecklistItems);

public sealed record AddJobNoteRequest(string Body);

public sealed record UpdateJobNoteRequest(string Body);

public sealed record AddJobPhotoRequest(
    string StoragePath,
    string? Caption,
    DateTimeOffset? TakenAt);

public sealed record ReorderJobChecklistRequest(
    List<Guid>? ChecklistItemIds);

public sealed class GetJobsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? AssignedUserProfileId { get; set; }
    public DateTimeOffset? ScheduledFrom { get; set; }
    public DateTimeOffset? ScheduledTo { get; set; }
}
