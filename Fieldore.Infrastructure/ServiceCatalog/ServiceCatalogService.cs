using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.ServiceCatalog.Contracts;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.ServiceCatalog;

public sealed class ServiceCatalogService(FieldoreDbContext dbContext) : IServiceCatalogService
{
    public async Task<ApiResponse<ServiceCatalogItemResponse>> CreateAsync(
        Guid userId,
        CreateServiceCatalogItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validation = ValidateRequest(request.Name, request.DefaultUnitPrice);
        if (validation is not null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, validation, 400);
        }

        var item = new ServiceCatalogItem
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            Name = request.Name.Trim(),
            Category = NormalizeOptional(request.Category),
            Description = NormalizeOptional(request.Description),
            DefaultUnitPrice = request.DefaultUnitPrice.HasValue ? NormalizeCurrency(request.DefaultUnitPrice.Value) : null,
            IsActive = true
        };

        dbContext.ServiceCatalogItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<ServiceCatalogItemResponse>.Create(MapToResponse(item), true, "Service item created", 201);
    }

    public async Task<ApiResponse<PagedResponse<ServiceCatalogItemResponse>>> GetAllAsync(
        Guid userId,
        GetServiceCatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<PagedResponse<ServiceCatalogItemResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var query = dbContext.ServiceCatalogItems
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value);

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(x => x.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || (x.Category != null && x.Category.Contains(search)));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pagedResponse = new PagedResponse<ServiceCatalogItemResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<ServiceCatalogItemResponse>>.Create(pagedResponse, true, "Service items retrieved", 200);
    }

    public async Task<ApiResponse<ServiceCatalogItemResponse>> GetByIdAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Business not found for user", 404);
        }

        var item = await dbContext.ServiceCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId && x.BusinessId == businessId.Value, cancellationToken);

        return item is null
            ? ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Service item not found", 404)
            : ApiResponse<ServiceCatalogItemResponse>.Create(MapToResponse(item), true, "Service item retrieved", 200);
    }

    public async Task<ApiResponse<ServiceCatalogItemResponse>> UpdateAsync(
        Guid userId,
        Guid itemId,
        UpdateServiceCatalogItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validation = ValidateRequest(request.Name, request.DefaultUnitPrice);
        if (validation is not null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, validation, 400);
        }

        var item = await dbContext.ServiceCatalogItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.BusinessId == businessId.Value, cancellationToken);

        if (item is null)
        {
            return ApiResponse<ServiceCatalogItemResponse>.Create(null, false, "Service item not found", 404);
        }

        item.Name = request.Name.Trim();
        item.Category = NormalizeOptional(request.Category);
        item.Description = NormalizeOptional(request.Description);
        item.DefaultUnitPrice = request.DefaultUnitPrice.HasValue ? NormalizeCurrency(request.DefaultUnitPrice.Value) : null;
        item.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<ServiceCatalogItemResponse>.Create(MapToResponse(item), true, "Service item updated", 200);
    }

    public async Task<ApiResponse<DeleteServiceCatalogItemResponse>> DeleteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteServiceCatalogItemResponse>.Create(null, false, "Business not found for user", 404);
        }

        var item = await dbContext.ServiceCatalogItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.BusinessId == businessId.Value, cancellationToken);

        if (item is null)
        {
            return ApiResponse<DeleteServiceCatalogItemResponse>.Create(null, false, "Service item not found", 404);
        }

        dbContext.ServiceCatalogItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<DeleteServiceCatalogItemResponse>.Create(
            new DeleteServiceCatalogItemResponse(item.Id, "Service item deleted"), true, "Service item deleted", 200);
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ServiceCatalogItemResponse MapToResponse(ServiceCatalogItem item) =>
        new(
            item.Id,
            item.Name,
            item.Category,
            item.Description,
            item.DefaultUnitPrice,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt);

    private static string? ValidateRequest(string name, decimal? defaultUnitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Service name is required";
        }

        if (name.Trim().Length > 200)
        {
            return "Service name must be 200 characters or less";
        }

        if (defaultUnitPrice is < 0)
        {
            return "Default unit price cannot be negative";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal NormalizeCurrency(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
