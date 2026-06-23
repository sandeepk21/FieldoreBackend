using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Estimates.Contracts;
using Fieldore.Application.Jobs.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Estimates;

public sealed class EstimateService(FieldoreDbContext dbContext, IJobService jobService) : IEstimateService
{
    public async Task<ApiResponse<EstimateResponse>> CreateAsync(
        Guid userId,
        CreateEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateEstimateRequest(
            request.Status, request.IssuedOn, request.ExpiresOn, request.TaxRate, request.DiscountAmount, request.LineItems);
        if (validationMessage is not null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, validationMessage, 400);
        }

        var customer = await GetCustomerAsync(businessId.Value, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Customer not found", 404);
        }

        var totals = CalculateTotals(request.LineItems, request.TaxRate, request.DiscountAmount);
        var estimate = new Estimate
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            CustomerId = request.CustomerId,
            EstimateNumber = await GenerateEstimateNumberAsync(businessId.Value, cancellationToken),
            Status = NormalizeStatus(request.Status),
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            TaxRate = NormalizeCurrency(request.TaxRate),
            DiscountAmount = totals.DiscountAmount,
            SubtotalAmount = totals.SubtotalAmount,
            TaxAmount = totals.TaxAmount,
            TotalAmount = totals.TotalAmount,
            DepositType = NormalizeDepositType(request.DepositType),
            DepositValue = NormalizeCurrency(Math.Max(0m, request.DepositValue)),
            DepositAmount = CalculateDeposit(totals.TotalAmount, request.DepositType, request.DepositValue),
            Title = NormalizeOptional(request.Title),
            Notes = NormalizeOptional(request.Notes),
            InternalNotes = NormalizeOptional(request.InternalNotes),
            CustomerNameSnapshot = BuildCustomerDisplayName(customer),
            CustomerEmailSnapshot = customer.Email,
            BillingAddressSnapshot = BuildBillingAddressSnapshot(customer, request.BillingAddress),
            LineItems = BuildLineItems(request.LineItems)
        };

        foreach (var lineItem in estimate.LineItems)
        {
            lineItem.EstimateId = estimate.Id;
        }

        dbContext.Estimates.Add(estimate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, estimate.Id, cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<EstimateResponse>>> GetAllAsync(
        Guid userId,
        GetEstimatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<PagedResponse<EstimateResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var query = dbContext.Estimates
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            query = query.Where(x => x.Status == status);
        }

        if (request.IssuedFrom.HasValue)
        {
            query = query.Where(x => x.IssuedOn >= request.IssuedFrom.Value);
        }

        if (request.IssuedTo.HasValue)
        {
            query = query.Where(x => x.IssuedOn <= request.IssuedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.EstimateNumber.ToLower().Contains(search) ||
                x.CustomerNameSnapshot.ToLower().Contains(search) ||
                (x.CustomerEmailSnapshot != null && x.CustomerEmailSnapshot.ToLower().Contains(search)));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var estimates = await query
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = await BuildEstimateResponsesAsync(estimates, cancellationToken);
        var pagedResponse = new PagedResponse<EstimateResponse>
        {
            Data = responses,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<EstimateResponse>>.Create(
            pagedResponse, true, "Estimates fetched successfully", 200);
    }

    public async Task<ApiResponse<EstimateResponse>> GetByIdAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        var estimate = await dbContext.Estimates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Estimate not found", 404);
        }

        var response = (await BuildEstimateResponsesAsync([estimate], cancellationToken)).Single();
        return ApiResponse<EstimateResponse>.Create(response, true, "Estimate fetched successfully", 200);
    }

    public async Task<ApiResponse<EstimateResponse>> UpdateAsync(
        Guid userId,
        Guid estimateId,
        UpdateEstimateRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateEstimateRequest(
            request.Status, request.IssuedOn, request.ExpiresOn, request.TaxRate, request.DiscountAmount, request.LineItems);
        if (validationMessage is not null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, validationMessage, 400);
        }

        var estimate = await dbContext.Estimates
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Estimate not found", 404);
        }

        if (estimate.Status == EstimateStatuses.Converted)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "A converted quote can no longer be edited", 409);
        }

        var customer = await GetCustomerAsync(businessId.Value, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Customer not found", 404);
        }

        var totals = CalculateTotals(request.LineItems, request.TaxRate, request.DiscountAmount);

        estimate.CustomerId = request.CustomerId;
        estimate.Status = NormalizeStatus(request.Status);
        estimate.IssuedOn = request.IssuedOn;
        estimate.ExpiresOn = request.ExpiresOn;
        estimate.TaxRate = NormalizeCurrency(request.TaxRate);
        estimate.DiscountAmount = totals.DiscountAmount;
        estimate.SubtotalAmount = totals.SubtotalAmount;
        estimate.TaxAmount = totals.TaxAmount;
        estimate.TotalAmount = totals.TotalAmount;
        estimate.DepositType = NormalizeDepositType(request.DepositType);
        estimate.DepositValue = NormalizeCurrency(Math.Max(0m, request.DepositValue));
        estimate.DepositAmount = CalculateDeposit(totals.TotalAmount, request.DepositType, request.DepositValue);
        estimate.Title = NormalizeOptional(request.Title);
        estimate.Notes = NormalizeOptional(request.Notes);
        estimate.InternalNotes = NormalizeOptional(request.InternalNotes);
        estimate.CustomerNameSnapshot = BuildCustomerDisplayName(customer);
        estimate.CustomerEmailSnapshot = customer.Email;
        estimate.BillingAddressSnapshot = BuildBillingAddressSnapshot(customer, request.BillingAddress);

        await dbContext.EstimateLineItems
            .Where(x => x.EstimateId == estimate.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var newLineItems = BuildLineItems(request.LineItems);
        foreach (var lineItem in newLineItems)
        {
            lineItem.EstimateId = estimate.Id;
        }

        await dbContext.EstimateLineItems.AddRangeAsync(newLineItems, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, estimate.Id, cancellationToken);
    }

    public async Task<ApiResponse<EstimateResponse>> UpdateStatusAsync(
        Guid userId,
        Guid estimateId,
        UpdateEstimateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        if (!IsValidStatus(request.Status))
        {
            return ApiResponse<EstimateResponse>.Create(
                null, false, "Estimate status must be draft, sent, approved, rejected, expired, or converted", 400);
        }

        var estimate = await dbContext.Estimates
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Estimate not found", 404);
        }

        estimate.Status = NormalizeStatus(request.Status);
        if (!string.IsNullOrWhiteSpace(request.ConvertedJobId) && Guid.TryParse(request.ConvertedJobId, out var jobGuid))
            estimate.ConvertedJobId = jobGuid;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, estimateId, cancellationToken);
    }

    public async Task<ApiResponse<DeleteEstimateResponse>> DeleteAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteEstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        var estimate = await dbContext.Estimates
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<DeleteEstimateResponse>.Create(null, false, "Estimate not found", 404);
        }

        if (estimate.ConvertedJobId is not null)
        {
            return ApiResponse<DeleteEstimateResponse>.Create(
                null, false, "Estimate cannot be deleted because it was converted to a job", 409);
        }

        await dbContext.EstimateLineItems
            .Where(x => x.EstimateId == estimateId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.EstimateAttachments
            .Where(x => x.EstimateId == estimateId)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.Estimates.Remove(estimate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DeleteEstimateResponse(estimateId, "Estimate deleted successfully");
        return ApiResponse<DeleteEstimateResponse>.Create(response, true, "Estimate deleted successfully", 200);
    }

    public async Task<ApiResponse<EstimateAttachmentResponse>> AddAttachmentAsync(
        Guid userId,
        Guid estimateId,
        AddEstimateAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateAttachmentResponse>.Create(null, false, "Business not found for user", 404);
        }

        var estimateExists = await dbContext.Estimates
            .AnyAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (!estimateExists)
        {
            return ApiResponse<EstimateAttachmentResponse>.Create(null, false, "Estimate not found", 404);
        }

        var attachment = new EstimateAttachment
        {
            Id = Guid.NewGuid(),
            EstimateId = estimateId,
            FileName = request.FileName,
            StoragePath = request.StoragePath,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            UploadedByUserId = request.UploadedByUserId
        };

        dbContext.EstimateAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new EstimateAttachmentResponse(
            attachment.Id, attachment.FileName, attachment.StoragePath,
            attachment.ContentType, attachment.FileSizeBytes, attachment.CreatedAt);

        return ApiResponse<EstimateAttachmentResponse>.Create(response, true, "Attachment added", 201);
    }

    public async Task<ApiResponse<DeleteEstimateAttachmentResponse>> DeleteAttachmentAsync(
        Guid userId,
        Guid estimateId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteEstimateAttachmentResponse>.Create(null, false, "Business not found for user", 404);
        }

        var ownsEstimate = await dbContext.Estimates
            .AnyAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (!ownsEstimate)
        {
            return ApiResponse<DeleteEstimateAttachmentResponse>.Create(null, false, "Estimate not found", 404);
        }

        var attachment = await dbContext.EstimateAttachments
            .FirstOrDefaultAsync(x => x.Id == attachmentId && x.EstimateId == estimateId, cancellationToken);

        if (attachment is null)
        {
            return ApiResponse<DeleteEstimateAttachmentResponse>.Create(null, false, "Attachment not found", 404);
        }

        dbContext.EstimateAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<DeleteEstimateAttachmentResponse>.Create(
            new DeleteEstimateAttachmentResponse(attachmentId, "Attachment deleted"), true, "Attachment deleted", 200);
    }

    public async Task<ApiResponse<EstimateResponse>> SendAsync(
        Guid userId,
        Guid estimateId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Business not found for user", 404);
        }

        var estimate = await dbContext.Estimates
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "Estimate not found", 404);
        }

        if (estimate.Status == EstimateStatuses.Converted)
        {
            return ApiResponse<EstimateResponse>.Create(null, false, "A converted quote can no longer be sent", 409);
        }

        estimate.PublicToken ??= Guid.NewGuid();
        estimate.Status = EstimateStatuses.Sent;
        estimate.SentAt = DateTimeOffset.UtcNow;
        estimate.RespondedAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, estimateId, cancellationToken);
    }

    public async Task<ApiResponse<ConvertEstimateToJobResponse>> ConvertToJobAsync(
        Guid userId,
        Guid estimateId,
        ConvertEstimateToJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<ConvertEstimateToJobResponse>.Create(null, false, "Business not found for user", 404);
        }

        var estimate = await dbContext.Estimates
            .FirstOrDefaultAsync(x => x.Id == estimateId && x.BusinessId == businessId.Value, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<ConvertEstimateToJobResponse>.Create(null, false, "Estimate not found", 404);
        }

        if (estimate.ConvertedJobId is not null)
        {
            return ApiResponse<ConvertEstimateToJobResponse>.Create(
                null, false, "Estimate has already been converted to a job", 409);
        }

        if (estimate.Status != EstimateStatuses.Approved)
        {
            return ApiResponse<ConvertEstimateToJobResponse>.Create(
                null, false, "Only an approved quote can be converted to a job", 400);
        }

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"{estimate.EstimateNumber} - {estimate.CustomerNameSnapshot}"
            : request.Title!.Trim();

        if (title.Length > 120)
        {
            title = title[..120];
        }

        var estimateLineItems = await dbContext.EstimateLineItems
            .AsNoTracking()
            .Where(x => x.EstimateId == estimate.Id)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var jobLineItems = estimateLineItems
            .Select(x => new JobLineItemRequest(x.SortOrder, x.ServiceName, x.Description, x.Quantity, x.UnitPrice))
            .ToList();

        var jobRequest = new CreateJobRequest(
            CustomerId: estimate.CustomerId,
            SourceLeadId: null,
            SourceEstimateId: estimate.Id,
            Title: title,
            JobType: null,
            Priority: JobPriorities.Normal,
            Status: JobStatuses.Scheduled,
            ScheduledStartAt: request.ScheduledStartAt ?? DateTimeOffset.UtcNow,
            ScheduledEndAt: null,
            ActualStartAt: null,
            ActualEndAt: null,
            EstimatedDurationMinutes: null,
            UseCustomerPrimaryAddress: true,
            ServiceAddress: null,
            Description: $"Created from quote {estimate.EstimateNumber}.",
            Assignments: null,
            ChecklistItems: null,
            LineItems: jobLineItems.Count > 0 ? jobLineItems : null);

        var jobResult = await jobService.CreateAsync(userId, jobRequest, cancellationToken);
        if (!jobResult.Success || jobResult.Data is null)
        {
            return ApiResponse<ConvertEstimateToJobResponse>.Create(
                null, false, jobResult.Message ?? "Failed to create job from estimate", jobResult.StatusCode);
        }

        estimate.Status = EstimateStatuses.Converted;
        estimate.ConvertedJobId = jobResult.Data.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ConvertEstimateToJobResponse(
            estimate.Id, jobResult.Data.Id, "Estimate converted to job successfully");
        return ApiResponse<ConvertEstimateToJobResponse>.Create(response, true, response.Message, 200);
    }

    public async Task<ApiResponse<PublicEstimateResponse>> GetPublicByTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default)
    {
        if (token == Guid.Empty)
        {
            return ApiResponse<PublicEstimateResponse>.Create(null, false, "Quote not found", 404);
        }

        var estimate = await dbContext.Estimates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicToken == token, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<PublicEstimateResponse>.Create(null, false, "Quote not found", 404);
        }

        var response = await BuildPublicEstimateResponseAsync(estimate, cancellationToken);
        return ApiResponse<PublicEstimateResponse>.Create(response, true, "Quote fetched successfully", 200);
    }

    public async Task<ApiResponse<PublicEstimateResponse>> RespondPublicAsync(
        Guid token,
        bool accept,
        CancellationToken cancellationToken = default)
    {
        if (token == Guid.Empty)
        {
            return ApiResponse<PublicEstimateResponse>.Create(null, false, "Quote not found", 404);
        }

        var estimate = await dbContext.Estimates
            .FirstOrDefaultAsync(x => x.PublicToken == token, cancellationToken);

        if (estimate is null)
        {
            return ApiResponse<PublicEstimateResponse>.Create(null, false, "Quote not found", 404);
        }

        if (estimate.Status != EstimateStatuses.Sent)
        {
            return ApiResponse<PublicEstimateResponse>.Create(
                null, false, "This quote has already been responded to", 409);
        }

        if (estimate.ExpiresOn.HasValue && estimate.ExpiresOn.Value < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ApiResponse<PublicEstimateResponse>.Create(null, false, "This quote has expired", 400);
        }

        estimate.Status = accept ? EstimateStatuses.Approved : EstimateStatuses.Rejected;
        estimate.RespondedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await BuildPublicEstimateResponseAsync(estimate, cancellationToken);
        var message = accept ? "Quote accepted" : "Quote rejected";
        return ApiResponse<PublicEstimateResponse>.Create(response, true, message, 200);
    }

    private async Task<PublicEstimateResponse> BuildPublicEstimateResponseAsync(
        Estimate estimate,
        CancellationToken cancellationToken)
    {
        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == estimate.BusinessId, cancellationToken);

        var lineItems = await dbContext.EstimateLineItems
            .AsNoTracking()
            .Where(x => x.EstimateId == estimate.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new EstimateLineItemResponse(
                x.Id, x.SortOrder, x.ServiceName, x.Description, x.Quantity, x.UnitPrice, x.LineTotal))
            .ToListAsync(cancellationToken);

        var notExpired = !estimate.ExpiresOn.HasValue || estimate.ExpiresOn.Value >= DateOnly.FromDateTime(DateTime.UtcNow);
        var canRespond = estimate.Status == EstimateStatuses.Sent && notExpired;

        return new PublicEstimateResponse(
            estimate.Id,
            estimate.EstimateNumber,
            estimate.Status,
            estimate.IssuedOn,
            estimate.ExpiresOn,
            business?.Name ?? "Your service provider",
            estimate.CustomerNameSnapshot,
            estimate.SubtotalAmount,
            estimate.DiscountAmount,
            estimate.TaxRate,
            estimate.TaxAmount,
            estimate.TotalAmount,
            string.IsNullOrWhiteSpace(business?.Currency) ? "USD" : business!.Currency,
            estimate.Notes,
            lineItems,
            canRespond,
            estimate.Title,
            estimate.DepositAmount);
    }

    private async Task<List<EstimateResponse>> BuildEstimateResponsesAsync(
        List<Estimate> estimates,
        CancellationToken cancellationToken)
    {
        if (estimates.Count == 0)
        {
            return [];
        }

        var estimateIds = estimates.Select(x => x.Id).ToList();
        var customerIds = estimates.Select(x => x.CustomerId).Distinct().ToList();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var lineItems = await dbContext.EstimateLineItems
            .AsNoTracking()
            .Where(x => estimateIds.Contains(x.EstimateId))
            .ToListAsync(cancellationToken);

        var attachments = await dbContext.EstimateAttachments
            .AsNoTracking()
            .Where(x => estimateIds.Contains(x.EstimateId))
            .ToListAsync(cancellationToken);

        return estimates
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .Select(estimate =>
            {
                customers.TryGetValue(estimate.CustomerId, out var customer);

                var responseLineItems = lineItems
                    .Where(x => x.EstimateId == estimate.Id)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.CreatedAt)
                    .Select(x => new EstimateLineItemResponse(
                        x.Id, x.SortOrder, x.ServiceName, x.Description, x.Quantity, x.UnitPrice, x.LineTotal))
                    .ToList();

                var responseAttachments = attachments
                    .Where(x => x.EstimateId == estimate.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new EstimateAttachmentResponse(
                        x.Id, x.FileName, x.StoragePath, x.ContentType, x.FileSizeBytes, x.CreatedAt))
                    .ToList();

                return new EstimateResponse(
                    estimate.Id,
                    estimate.BusinessId,
                    estimate.CustomerId,
                    estimate.EstimateNumber,
                    estimate.Status,
                    estimate.IssuedOn,
                    estimate.ExpiresOn,
                    estimate.TaxRate,
                    estimate.DiscountAmount,
                    estimate.SubtotalAmount,
                    estimate.TaxAmount,
                    estimate.TotalAmount,
                    estimate.Notes,
                    estimate.CustomerNameSnapshot,
                    estimate.CustomerEmailSnapshot,
                    MapAddress(estimate.BillingAddressSnapshot),
                    customer is null
                        ? null
                        : new EstimateCustomerSummaryResponse(
                            customer.Id,
                            BuildCustomerDisplayName(customer),
                            customer.Email,
                            customer.MobilePhone),
                    responseLineItems,
                    estimate.PublicToken,
                    estimate.SentAt,
                    estimate.RespondedAt,
                    estimate.ConvertedJobId,
                    estimate.CreatedAt,
                    estimate.UpdatedAt,
                    estimate.Title,
                    estimate.InternalNotes,
                    estimate.DepositType,
                    estimate.DepositValue,
                    estimate.DepositAmount,
                    responseAttachments);
            })
            .ToList();
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

    private async Task<string> GenerateEstimateNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var existingNumbers = await dbContext.Estimates
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .Select(x => x.EstimateNumber)
            .ToListAsync(cancellationToken);

        var sequence = existingNumbers
            .Select(ParseEstimateSequence)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var estimateNumber = FormatEstimateNumber(sequence);

        while (existingNumbers.Contains(estimateNumber, StringComparer.OrdinalIgnoreCase))
        {
            sequence++;
            estimateNumber = FormatEstimateNumber(sequence);
        }

        return estimateNumber;
    }

    private static string? ValidateEstimateRequest(
        string status,
        DateOnly issuedOn,
        DateOnly? expiresOn,
        decimal taxRate,
        decimal discountAmount,
        List<EstimateLineItemRequest>? lineItems)
    {
        if (!IsValidStatus(status))
        {
            return "Estimate status must be draft, sent, approved, rejected, expired, or converted";
        }

        if (expiresOn.HasValue && expiresOn.Value < issuedOn)
        {
            return "Expiry date must be on or after issue date";
        }

        if (taxRate < 0)
        {
            return "Tax rate cannot be negative";
        }

        if (discountAmount < 0)
        {
            return "Discount amount cannot be negative";
        }

        if (lineItems is null || lineItems.Count == 0)
        {
            return "At least one estimate line item is required";
        }

        if (lineItems.Any(x => string.IsNullOrWhiteSpace(x.ServiceName)))
        {
            return "Estimate line item service name is required";
        }

        if (lineItems.Any(x => x.Quantity <= 0))
        {
            return "Estimate line item quantity must be greater than zero";
        }

        if (lineItems.Any(x => x.UnitPrice < 0))
        {
            return "Estimate line item unit price cannot be negative";
        }

        return null;
    }

    private static List<EstimateLineItem> BuildLineItems(List<EstimateLineItemRequest>? lineItems)
    {
        return lineItems?
            .Select(x =>
            {
                var quantity = NormalizeCurrency(x.Quantity);
                var unitPrice = NormalizeCurrency(x.UnitPrice);

                return new EstimateLineItem
                {
                    Id = Guid.NewGuid(),
                    SortOrder = x.SortOrder,
                    ServiceName = x.ServiceName.Trim(),
                    Description = NormalizeOptional(x.Description),
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    LineTotal = NormalizeCurrency(quantity * unitPrice)
                };
            })
            .ToList() ?? [];
    }

    private static EstimateTotals CalculateTotals(
        List<EstimateLineItemRequest>? lineItems,
        decimal taxRate,
        decimal discountAmount)
    {
        var normalizedTaxRate = NormalizeCurrency(taxRate);
        var normalizedDiscount = NormalizeCurrency(discountAmount);
        var subtotal = NormalizeCurrency(lineItems?.Sum(x => NormalizeCurrency(x.Quantity) * NormalizeCurrency(x.UnitPrice)) ?? 0m);
        var taxableAmount = Math.Max(0m, subtotal - normalizedDiscount);
        var taxAmount = NormalizeCurrency(taxableAmount * normalizedTaxRate / 100m);
        var totalAmount = NormalizeCurrency(taxableAmount + taxAmount);

        return new EstimateTotals(subtotal, normalizedDiscount, taxAmount, totalAmount);
    }

    private static Address? BuildBillingAddressSnapshot(Customer customer, EstimateAddressRequest? request)
    {
        if (request is not null && HasAddressData(request))
        {
            return new Address
            {
                Line1 = NormalizeOptional(request.Line1),
                Line2 = NormalizeOptional(request.Line2),
                City = NormalizeOptional(request.City),
                StateOrProvince = NormalizeOptional(request.StateOrProvince),
                PostalCode = NormalizeOptional(request.PostalCode),
                Country = NormalizeOptional(request.Country),
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };
        }

        var address = customer.Addresses
            .OrderByDescending(x => x.IsBilling)
            .ThenByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.Address)
            .FirstOrDefault();

        return address is null
            ? null
            : new Address
            {
                Line1 = address.Line1,
                Line2 = address.Line2,
                City = address.City,
                StateOrProvince = address.StateOrProvince,
                PostalCode = address.PostalCode,
                Country = address.Country,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };
    }

    private static bool HasAddressData(EstimateAddressRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Line1) ||
               !string.IsNullOrWhiteSpace(request.Line2) ||
               !string.IsNullOrWhiteSpace(request.City) ||
               !string.IsNullOrWhiteSpace(request.StateOrProvince) ||
               !string.IsNullOrWhiteSpace(request.PostalCode) ||
               !string.IsNullOrWhiteSpace(request.Country) ||
               request.Latitude.HasValue ||
               request.Longitude.HasValue;
    }

    private static EstimateAddressResponse? MapAddress(Address? address)
    {
        return address is null
            ? null
            : new EstimateAddressResponse(
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

    private static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is EstimateStatuses.Draft
            or EstimateStatuses.Sent
            or EstimateStatuses.Approved
            or EstimateStatuses.Rejected
            or EstimateStatuses.Expired
            or EstimateStatuses.Converted;
    }

    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? EstimateStatuses.Draft
            : status.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal NormalizeCurrency(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static int ParseEstimateSequence(string estimateNumber)
    {
        if (string.IsNullOrWhiteSpace(estimateNumber))
        {
            return 0;
        }

        var parts = estimateNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lastPart = parts.LastOrDefault();
        return int.TryParse(lastPart, out var sequence) ? sequence : 0;
    }

    private static string FormatEstimateNumber(int sequence) => $"EST-{sequence:D6}";

    private static string NormalizeDepositType(string? depositType)
    {
        var normalized = (depositType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is EstimateDepositTypes.Percent or EstimateDepositTypes.Fixed
            ? normalized
            : EstimateDepositTypes.None;
    }

    // Resolves the upfront deposit to a currency amount, never exceeding the total.
    private static decimal CalculateDeposit(decimal total, string? depositType, decimal depositValue)
    {
        var value = NormalizeCurrency(Math.Max(0m, depositValue));
        var amount = NormalizeDepositType(depositType) switch
        {
            EstimateDepositTypes.Percent => NormalizeCurrency(total * value / 100m),
            EstimateDepositTypes.Fixed => value,
            _ => 0m
        };

        return Math.Min(Math.Max(0m, amount), Math.Max(0m, total));
    }

    private sealed record EstimateTotals(
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount);
}
