using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Jobs;

public sealed class JobService(FieldoreDbContext dbContext) : IJobService
{
    public async Task<ApiResponse<JobResponse>> CreateAsync(
        Guid userId,
        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateJobRequest(
            request.Title,
            request.Priority,
            request.Status,
            request.ScheduledStartAt,
            request.ScheduledEndAt,
            request.ActualStartAt,
            request.ActualEndAt,
            request.EstimatedDurationMinutes,
            request.UseCustomerPrimaryAddress,
            request.ServiceAddress,
            request.Assignments,
            request.ChecklistItems);

        if (validationMessage is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, validationMessage, 400);
        }

        var customer = await GetCustomerAsync(business.Id, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Customer not found", 404);
        }

        var eligibleProfiles = await GetEligibleProfilesAsync(business.Id, business.AuthUserId, cancellationToken);
        var assignmentValidation = ValidateAssignments(request.Assignments, eligibleProfiles.Keys.ToHashSet());
        if (assignmentValidation is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, assignmentValidation, 400);
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            CustomerId = customer.Id,
            SourceLeadId = request.SourceLeadId,
            JobNumber = await GenerateJobNumberAsync(business.Id, cancellationToken),
            Title = request.Title.Trim(),
            JobType = NormalizeOptional(request.JobType),
            Priority = NormalizePriority(request.Priority),
            Status = NormalizeStatus(request.Status),
            ScheduledStartAt = request.ScheduledStartAt,
            ScheduledEndAt = request.ScheduledEndAt,
            ActualStartAt = request.ActualStartAt,
            ActualEndAt = request.ActualEndAt,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            UseCustomerPrimaryAddress = request.UseCustomerPrimaryAddress,
            ServiceAddress = BuildServiceAddress(request.UseCustomerPrimaryAddress, request.ServiceAddress, customer),
            Description = NormalizeOptional(request.Description)
        };

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        await ReplaceAssignmentsInternalAsync(job.Id, request.Assignments, cancellationToken);
        await ReplaceChecklistInternalAsync(job.Id, request.ChecklistItems, userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, job.Id, cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<JobResponse>>> GetAllAsync(
        Guid userId,
        GetJobsRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<PagedResponse<JobResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var query = dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.BusinessId == business.Id);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = NormalizePriority(request.Priority);
            query = query.Where(x => x.Priority == priority);
        }

        if (request.ScheduledFrom.HasValue)
        {
            query = query.Where(x => x.ScheduledStartAt >= request.ScheduledFrom.Value);
        }

        if (request.ScheduledTo.HasValue)
        {
            query = query.Where(x => x.ScheduledStartAt <= request.ScheduledTo.Value);
        }

        if (request.AssignedUserProfileId.HasValue)
        {
            var assignedJobIds = dbContext.JobAssignments
                .Where(x => x.UserProfileId == request.AssignedUserProfileId.Value)
                .Select(x => x.JobId);
            query = query.Where(x => assignedJobIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            var customerIds = await dbContext.Customers
                .AsNoTracking()
                .Where(x => x.BusinessId == business.Id)
                .Where(x =>
                    (x.FirstName + " " + x.LastName).ToLower().Contains(search) ||
                    x.MobilePhone.Contains(search) ||
                    (x.CompanyName != null && x.CompanyName.ToLower().Contains(search)))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(x =>
                x.JobNumber.ToLower().Contains(search) ||
                x.Title.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)) ||
                customerIds.Contains(x.CustomerId));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var jobs = await query
            .OrderByDescending(x => x.ScheduledStartAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = await BuildJobResponsesAsync(jobs, cancellationToken);

        var pagedResponse = new PagedResponse<JobResponse>
        {
            Data = responses,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<JobResponse>>.Create(
            pagedResponse, true, "Jobs fetched successfully", 200);
    }

    public async Task<ApiResponse<JobResponse>> GetByIdAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var job = await dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (job is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Job not found", 404);
        }

        var response = (await BuildJobResponsesAsync([job], cancellationToken)).Single();
        return ApiResponse<JobResponse>.Create(response, true, "Job fetched successfully", 200);
    }

    public async Task<ApiResponse<JobResponse>> UpdateAsync(
        Guid userId,
        Guid jobId,
        UpdateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateJobRequest(
            request.Title,
            request.Priority,
            request.Status,
            request.ScheduledStartAt,
            request.ScheduledEndAt,
            request.ActualStartAt,
            request.ActualEndAt,
            request.EstimatedDurationMinutes,
            request.UseCustomerPrimaryAddress,
            request.ServiceAddress,
            request.Assignments,
            request.ChecklistItems);

        if (validationMessage is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, validationMessage, 400);
        }

        var job = await dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.BusinessId == business.Id, cancellationToken);

        if (job is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Job not found", 404);
        }

        var customer = await GetCustomerAsync(business.Id, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Customer not found", 404);
        }

        var eligibleProfiles = await GetEligibleProfilesAsync(business.Id, business.AuthUserId, cancellationToken);
        var assignmentValidation = ValidateAssignments(request.Assignments, eligibleProfiles.Keys.ToHashSet());
        if (assignmentValidation is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, assignmentValidation, 400);
        }

        job.CustomerId = request.CustomerId;
        job.SourceLeadId = request.SourceLeadId;
        job.Title = request.Title.Trim();
        job.JobType = NormalizeOptional(request.JobType);
        job.Priority = NormalizePriority(request.Priority);
        job.Status = NormalizeStatus(request.Status);
        job.ScheduledStartAt = request.ScheduledStartAt;
        job.ScheduledEndAt = request.ScheduledEndAt;
        job.ActualStartAt = request.ActualStartAt;
        job.ActualEndAt = request.ActualEndAt;
        job.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
        job.UseCustomerPrimaryAddress = request.UseCustomerPrimaryAddress;
        job.ServiceAddress = BuildServiceAddress(request.UseCustomerPrimaryAddress, request.ServiceAddress, customer);
        job.Description = NormalizeOptional(request.Description);

        await ReplaceAssignmentsInternalAsync(job.Id, request.Assignments, cancellationToken);
        await ReplaceChecklistInternalAsync(job.Id, request.ChecklistItems, userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, job.Id, cancellationToken);
    }

    public async Task<ApiResponse<DeleteJobResponse>> DeleteAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteJobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var job = await dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (job is null)
        {
            return ApiResponse<DeleteJobResponse>.Create(null, false, "Job not found", 404);
        }

        await dbContext.JobAssignments.Where(x => x.JobId == jobId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.JobChecklistItems.Where(x => x.JobId == jobId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.JobNotes.Where(x => x.JobId == jobId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.JobPhotos.Where(x => x.JobId == jobId).ExecuteDeleteAsync(cancellationToken);

        dbContext.Jobs.Remove(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DeleteJobResponse(jobId, "Job deleted successfully");
        return ApiResponse<DeleteJobResponse>.Create(response, true, "Job deleted successfully", 200);
    }

    public async Task<ApiResponse<JobResponse>> UpdateStatusAsync(
        Guid userId,
        Guid jobId,
        UpdateJobStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var job = await dbContext.Jobs
            .FirstOrDefaultAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (job is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Job not found", 404);
        }

        var validationMessage = ValidateStatusOnly(request.Status, request.ActualStartAt, request.ActualEndAt);
        if (validationMessage is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, validationMessage, 400);
        }

        job.Status = NormalizeStatus(request.Status);
        job.ActualStartAt = request.ActualStartAt;
        job.ActualEndAt = request.ActualEndAt;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, jobId, cancellationToken);
    }

    public async Task<ApiResponse<JobResponse>> ReplaceAssignmentsAsync(
        Guid userId,
        Guid jobId,
        ReplaceJobAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var jobExists = await dbContext.Jobs
            .AnyAsync(x => x.Id == jobId && x.BusinessId == business.Id, cancellationToken);

        if (!jobExists)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Job not found", 404);
        }

        var eligibleProfiles = await GetEligibleProfilesAsync(business.Id, business.AuthUserId, cancellationToken);
        var assignmentValidation = ValidateAssignments(request.Assignments, eligibleProfiles.Keys.ToHashSet());
        if (assignmentValidation is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, assignmentValidation, 400);
        }

        await ReplaceAssignmentsInternalAsync(jobId, request.Assignments, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, jobId, cancellationToken);
    }

    public async Task<ApiResponse<JobResponse>> ReplaceChecklistAsync(
        Guid userId,
        Guid jobId,
        ReplaceJobChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var jobExists = await dbContext.Jobs
            .AnyAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (!jobExists)
        {
            return ApiResponse<JobResponse>.Create(null, false, "Job not found", 404);
        }

        var checklistValidation = ValidateChecklistItems(request.ChecklistItems);
        if (checklistValidation is not null)
        {
            return ApiResponse<JobResponse>.Create(null, false, checklistValidation, 400);
        }

        await ReplaceChecklistInternalAsync(jobId, request.ChecklistItems, userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, jobId, cancellationToken);
    }

    public async Task<ApiResponse<JobNoteResponse>> AddNoteAsync(
        Guid userId,
        Guid jobId,
        AddJobNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<JobNoteResponse>.Create(null, false, "Business not found for user", 404);
        }

        var jobExists = await dbContext.Jobs
            .AnyAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (!jobExists)
        {
            return ApiResponse<JobNoteResponse>.Create(null, false, "Job not found", 404);
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ApiResponse<JobNoteResponse>.Create(null, false, "Note body is required", 400);
        }

        var userProfile = await GetUserProfileAsync(userId, cancellationToken);
        var note = new JobNote
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CreatedByUserId = userProfile?.Id,
            Body = request.Body.Trim()
        };

        dbContext.JobNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new JobNoteResponse(
            note.Id,
            note.CreatedByUserId,
            note.Body,
            note.CreatedAt,
            BuildProfileDisplayName(userProfile));

        return ApiResponse<JobNoteResponse>.Create(response, true, "Job note added successfully", 201);
    }

    public async Task<ApiResponse<JobPhotoResponse>> AddPhotoAsync(
        Guid userId,
        Guid jobId,
        AddJobPhotoRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<JobPhotoResponse>.Create(null, false, "Business not found for user", 404);
        }

        var jobExists = await dbContext.Jobs
            .AnyAsync(x => x.Id == jobId && x.BusinessId == businessId.Value, cancellationToken);

        if (!jobExists)
        {
            return ApiResponse<JobPhotoResponse>.Create(null, false, "Job not found", 404);
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            return ApiResponse<JobPhotoResponse>.Create(null, false, "Storage path is required", 400);
        }

        var userProfile = await GetUserProfileAsync(userId, cancellationToken);
        var photo = new JobPhoto
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            UploadedByUserId = userProfile?.Id,
            StoragePath = request.StoragePath.Trim(),
            Caption = NormalizeOptional(request.Caption),
            TakenAt = request.TakenAt
        };

        dbContext.JobPhotos.Add(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new JobPhotoResponse(
            photo.Id,
            photo.UploadedByUserId,
            photo.StoragePath,
            photo.Caption,
            photo.TakenAt,
            photo.CreatedAt);

        return ApiResponse<JobPhotoResponse>.Create(response, true, "Job photo added successfully", 201);
    }

    private async Task<List<JobResponse>> BuildJobResponsesAsync(
        List<Job> jobs,
        CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
        {
            return [];
        }

        var jobIds = jobs.Select(x => x.Id).ToList();
        var customerIds = jobs.Select(x => x.CustomerId).Distinct().ToList();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var assignments = await dbContext.JobAssignments
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId))
            .ToListAsync(cancellationToken);

        var checklistItems = await dbContext.JobChecklistItems
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId))
            .ToListAsync(cancellationToken);

        var notes = await dbContext.JobNotes
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId))
            .ToListAsync(cancellationToken);

        var photos = await dbContext.JobPhotos
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.JobId))
            .ToListAsync(cancellationToken);

        var profileIds = assignments.Select(x => x.UserProfileId)
            .Concat(notes.Where(x => x.CreatedByUserId.HasValue).Select(x => x.CreatedByUserId!.Value))
            .Distinct()
            .ToList();

        var profiles = profileIds.Count == 0
            ? new Dictionary<Guid, AppUserProfile>()
            : await dbContext.UserProfiles
                .AsNoTracking()
                .Where(x => profileIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        return jobs
            .OrderByDescending(x => x.ScheduledStartAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(job =>
            {
                customers.TryGetValue(job.CustomerId, out var customer);

                var jobAssignments = assignments
                    .Where(x => x.JobId == job.Id)
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => profiles.TryGetValue(x.UserProfileId, out var profile)
                        ? BuildProfileDisplayName(profile)
                        : string.Empty)
                    .Select(x =>
                    {
                        profiles.TryGetValue(x.UserProfileId, out var profile);
                        return new JobAssignmentResponse(
                            x.Id,
                            x.UserProfileId,
                            x.IsPrimary,
                            BuildProfileDisplayName(profile),
                            profile?.Email);
                    })
                    .ToList();

                var jobChecklist = checklistItems
                    .Where(x => x.JobId == job.Id)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.CreatedAt)
                    .Select(x => new JobChecklistItemResponse(
                        x.Id,
                        x.SortOrder,
                        x.TaskName,
                        x.IsCompleted,
                        x.CompletedAt,
                        x.CompletedByUserId))
                    .ToList();

                var jobNotes = notes
                    .Where(x => x.JobId == job.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x =>
                    {
                        AppUserProfile? noteProfile = null;
                        if (x.CreatedByUserId.HasValue)
                        {
                            profiles.TryGetValue(x.CreatedByUserId.Value, out noteProfile);
                        }

                        return new JobNoteResponse(
                            x.Id,
                            x.CreatedByUserId,
                            x.Body,
                            x.CreatedAt,
                            BuildProfileDisplayName(noteProfile));
                    })
                    .ToList();

                var jobPhotos = photos
                    .Where(x => x.JobId == job.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new JobPhotoResponse(
                        x.Id,
                        x.UploadedByUserId,
                        x.StoragePath,
                        x.Caption,
                        x.TakenAt,
                        x.CreatedAt))
                    .ToList();

                return new JobResponse(
                    job.Id,
                    job.BusinessId,
                    job.CustomerId,
                    job.SourceLeadId,
                    job.JobNumber,
                    job.Title,
                    job.JobType,
                    job.Priority,
                    job.Status,
                    job.ScheduledStartAt,
                    job.ScheduledEndAt,
                    job.ActualStartAt,
                    job.ActualEndAt,
                    job.EstimatedDurationMinutes,
                    job.UseCustomerPrimaryAddress,
                    MapAddress(job.ServiceAddress),
                    job.Description,
                    customer is null ? null : new JobCustomerSummaryResponse(
                        customer.Id,
                        BuildCustomerDisplayName(customer),
                        customer.MobilePhone,
                        customer.Email),
                    jobAssignments,
                    jobChecklist,
                    jobNotes,
                    jobPhotos,
                    job.CreatedAt,
                    job.UpdatedAt);
            })
            .ToList();
    }

    private async Task ReplaceAssignmentsInternalAsync(
        Guid jobId,
        List<JobAssignmentRequest>? assignments,
        CancellationToken cancellationToken)
    {
        await dbContext.JobAssignments
            .Where(x => x.JobId == jobId)
            .ExecuteDeleteAsync(cancellationToken);

        if (assignments is null || assignments.Count == 0)
        {
            return;
        }

        var newAssignments = assignments
            .Select(x => new JobAssignment
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                UserProfileId = x.UserProfileId,
                IsPrimary = x.IsPrimary
            })
            .ToList();

        await dbContext.JobAssignments.AddRangeAsync(newAssignments, cancellationToken);
    }

    private async Task ReplaceChecklistInternalAsync(
        Guid jobId,
        List<JobChecklistItemRequest>? checklistItems,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await dbContext.JobChecklistItems
            .Where(x => x.JobId == jobId)
            .ExecuteDeleteAsync(cancellationToken);

        if (checklistItems is null || checklistItems.Count == 0)
        {
            return;
        }

        var userProfile = await GetUserProfileAsync(userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var items = checklistItems
            .Select(x => new JobChecklistItem
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                SortOrder = x.SortOrder,
                TaskName = x.TaskName.Trim(),
                IsCompleted = x.IsCompleted,
                CompletedAt = x.IsCompleted ? now : null,
                CompletedByUserId = x.IsCompleted ? userProfile?.Id : null
            })
            .ToList();

        await dbContext.JobChecklistItems.AddRangeAsync(items, cancellationToken);
    }

    private async Task<Business?> GetBusinessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .FirstOrDefaultAsync(x => x.AuthUserId == userId, cancellationToken);
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Customer?> GetCustomerAsync(Guid businessId, Guid customerId, CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(
                x => x.Id == customerId && x.BusinessId == businessId && x.IsActive,
                cancellationToken);
    }

    private async Task<AppUserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AuthUserId == userId && x.IsActive, cancellationToken);
    }

    private async Task<Dictionary<Guid, AppUserProfile>> GetEligibleProfilesAsync(
        Guid businessId,
        Guid ownerAuthUserId,
        CancellationToken cancellationToken)
    {
        var membershipUserIds = await dbContext.BusinessMemberships
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.IsActive)
            .Select(x => x.UserProfileId)
            .ToListAsync(cancellationToken);

        var ownerProfileId = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.AuthUserId == ownerAuthUserId && x.IsActive)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerProfileId.HasValue)
        {
            membershipUserIds.Add(ownerProfileId.Value);
        }

        var uniqueProfileIds = membershipUserIds.Distinct().ToList();
        if (uniqueProfileIds.Count == 0)
        {
            return new Dictionary<Guid, AppUserProfile>();
        }

        return await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => uniqueProfileIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    private async Task<string> GenerateJobNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var existingNumbers = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .Select(x => x.JobNumber)
            .ToListAsync(cancellationToken);

        var sequence = existingNumbers
            .Select(ParseJobSequence)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var jobNumber = FormatJobNumber(sequence);

        while (existingNumbers.Contains(jobNumber, StringComparer.OrdinalIgnoreCase))
        {
            sequence++;
            jobNumber = FormatJobNumber(sequence);
        }

        return jobNumber;
    }

    private static int ParseJobSequence(string jobNumber)
    {
        if (string.IsNullOrWhiteSpace(jobNumber))
        {
            return 0;
        }

        var parts = jobNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lastPart = parts.LastOrDefault();
        return int.TryParse(lastPart, out var sequence) ? sequence : 0;
    }

    private static string FormatJobNumber(int sequence) => $"JOB-{sequence:D6}";

    private static string? ValidateJobRequest(
        string title,
        string priority,
        string status,
        DateTimeOffset scheduledStartAt,
        DateTimeOffset? scheduledEndAt,
        DateTimeOffset? actualStartAt,
        DateTimeOffset? actualEndAt,
        int? estimatedDurationMinutes,
        bool useCustomerPrimaryAddress,
        JobAddressRequest? serviceAddress,
        List<JobAssignmentRequest>? assignments,
        List<JobChecklistItemRequest>? checklistItems)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Job title is required";
        }

        if (estimatedDurationMinutes.HasValue && estimatedDurationMinutes.Value <= 0)
        {
            return "Estimated duration must be greater than zero";
        }

        var statusValidation = ValidateStatusOnly(status, actualStartAt, actualEndAt);
        if (statusValidation is not null)
        {
            return statusValidation;
        }

        if (!IsValidPriority(priority))
        {
            return "Job priority must be normal or urgent";
        }

        if (scheduledEndAt.HasValue && scheduledEndAt.Value < scheduledStartAt)
        {
            return "Scheduled end time must be after scheduled start time";
        }

        if (!useCustomerPrimaryAddress)
        {
            var addressValidation = ValidateAddress(serviceAddress);
            if (addressValidation is not null)
            {
                return addressValidation;
            }
        }

        var assignmentValidation = ValidateAssignments(assignments, null);
        if (assignmentValidation is not null)
        {
            return assignmentValidation;
        }

        return ValidateChecklistItems(checklistItems);
    }

    private static string? ValidateStatusOnly(
        string status,
        DateTimeOffset? actualStartAt,
        DateTimeOffset? actualEndAt)
    {
        if (!IsValidStatus(status))
        {
            return "Job status must be draft, scheduled, in_progress, completed, or cancelled";
        }

        if (actualStartAt.HasValue && actualEndAt.HasValue && actualEndAt.Value < actualStartAt.Value)
        {
            return "Actual end time must be after actual start time";
        }

        return null;
    }

    private static string? ValidateAssignments(List<JobAssignmentRequest>? assignments, HashSet<Guid>? validProfileIds)
    {
        if (assignments is null || assignments.Count == 0)
        {
            return null;
        }

        if (assignments.Count(x => x.IsPrimary) > 1)
        {
            return "Only one primary assignee is allowed";
        }

        var duplicateUserIds = assignments
            .GroupBy(x => x.UserProfileId)
            .Any(x => x.Count() > 1);

        if (duplicateUserIds)
        {
            return "Duplicate assignees are not allowed";
        }

        if (validProfileIds is not null && assignments.Any(x => !validProfileIds.Contains(x.UserProfileId)))
        {
            return "One or more assignees are not part of this business";
        }

        return null;
    }

    private static string? ValidateChecklistItems(List<JobChecklistItemRequest>? checklistItems)
    {
        if (checklistItems is null || checklistItems.Count == 0)
        {
            return null;
        }

        if (checklistItems.Any(x => string.IsNullOrWhiteSpace(x.TaskName)))
        {
            return "Checklist task name is required";
        }

        return null;
    }

    private static string? ValidateAddress(JobAddressRequest? address)
    {
        if (address is null)
        {
            return "Service address is required when customer primary address is not used";
        }

        if (string.IsNullOrWhiteSpace(address.Line1) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.StateOrProvince) ||
            string.IsNullOrWhiteSpace(address.PostalCode) ||
            string.IsNullOrWhiteSpace(address.Country))
        {
            return "Service address must include line1, city, state, postal code, and country";
        }

        return null;
    }

    private static bool IsValidPriority(string priority)
    {
        var normalized = NormalizePriority(priority);
        return normalized is JobPriorities.Normal or JobPriorities.Urgent;
    }

    private static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is JobStatuses.Draft
            or JobStatuses.Scheduled
            or JobStatuses.InProgress
            or JobStatuses.Completed
            or JobStatuses.Cancelled;
    }

    private static string NormalizePriority(string priority)
    {
        return string.IsNullOrWhiteSpace(priority)
            ? JobPriorities.Normal
            : priority.Trim().ToLowerInvariant();
    }

    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? JobStatuses.Draft
            : status.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Address BuildServiceAddress(
        bool useCustomerPrimaryAddress,
        JobAddressRequest? request,
        Customer customer)
    {
        if (!useCustomerPrimaryAddress && request is not null)
        {
            return new Address
            {
                Line1 = request.Line1.Trim(),
                Line2 = NormalizeOptional(request.Line2),
                City = request.City.Trim(),
                StateOrProvince = request.StateOrProvince.Trim(),
                PostalCode = request.PostalCode.Trim(),
                Country = request.Country.Trim(),
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };
        }

        var primaryAddress = customer.Addresses
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefault();

        return new Address
        {
            Line1 = primaryAddress?.Address.Line1,
            Line2 = primaryAddress?.Address.Line2,
            City = primaryAddress?.Address.City,
            StateOrProvince = primaryAddress?.Address.StateOrProvince,
            PostalCode = primaryAddress?.Address.PostalCode,
            Country = primaryAddress?.Address.Country,
            Latitude = primaryAddress?.Address.Latitude,
            Longitude = primaryAddress?.Address.Longitude
        };
    }

    private static JobAddressResponse? MapAddress(Address? address)
    {
        return address is null
            ? null
            : new JobAddressResponse(
                address.Line1,
                address.Line2,
                address.City,
                address.StateOrProvince,
                address.PostalCode,
                address.Country,
                address.Latitude,
                address.Longitude);
    }

    private static string BuildCustomerDisplayName(Customer customer)
    {
        var fullName = $"{customer.FirstName} {customer.LastName}".Trim();
        return string.IsNullOrWhiteSpace(customer.CompanyName)
            ? fullName
            : $"{customer.CompanyName.Trim()} - {fullName}";
    }

    private static string BuildProfileDisplayName(AppUserProfile? profile)
    {
        if (profile is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            return profile.DisplayName.Trim();
        }

        return $"{profile.FirstName} {profile.LastName}".Trim();
    }
}
