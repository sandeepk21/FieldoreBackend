using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Workers.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Workers;

public sealed class WorkerService(FieldoreDbContext dbContext) : IWorkerService
{
    private static readonly HashSet<string> ManagedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        BusinessMembershipRoles.Admin,
        BusinessMembershipRoles.Manager,
        BusinessMembershipRoles.Technician,
        BusinessMembershipRoles.Staff,
    };

    public async Task<ApiResponse<List<WorkerResponse>>> GetAllAsync(
        Guid userId,
        GetWorkersRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<List<WorkerResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var query = dbContext.BusinessMemberships
            .AsNoTracking()
            .Where(m => m.BusinessId == business.Id && m.Role != BusinessMembershipRoles.Owner)
            .Join(
                dbContext.UserProfiles.AsNoTracking(),
                m => m.UserProfileId,
                p => p.Id,
                (m, p) => new { Membership = m, Profile = p });

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.Profile.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Profile.FirstName.Contains(search) ||
                x.Profile.LastName.Contains(search) ||
                (x.Profile.Email != null && x.Profile.Email.Contains(search)));
        }

        var results = await query
            .OrderBy(x => x.Profile.FirstName)
            .ThenBy(x => x.Profile.LastName)
            .ToListAsync(cancellationToken);

        var responses = results.Select(x => MapToResponse(x.Profile, x.Membership)).ToList();
        return ApiResponse<List<WorkerResponse>>.Create(responses, true, "Workers retrieved", 200);
    }

    public async Task<ApiResponse<List<WorkerResponse>>> GetAssignableAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<List<WorkerResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var ownerProfile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AuthUserId == business.AuthUserId, cancellationToken);

        var members = await dbContext.BusinessMemberships
            .AsNoTracking()
            .Where(m => m.BusinessId == business.Id && m.IsActive && m.Role != BusinessMembershipRoles.Owner)
            .Join(
                dbContext.UserProfiles.AsNoTracking().Where(p => p.IsActive),
                m => m.UserProfileId,
                p => p.Id,
                (m, p) => new { Membership = m, Profile = p })
            .OrderBy(x => x.Profile.FirstName)
            .ThenBy(x => x.Profile.LastName)
            .ToListAsync(cancellationToken);

        var responses = new List<WorkerResponse>();

        if (ownerProfile is not null)
        {
            responses.Add(new WorkerResponse(
                ownerProfile.Id,
                null,
                BuildDisplayName(ownerProfile),
                ownerProfile.FirstName,
                ownerProfile.LastName,
                ownerProfile.Email,
                ownerProfile.Phone,
                BusinessMembershipRoles.Owner,
                ownerProfile.IsActive,
                ownerProfile.CreatedAt));
        }

        responses.AddRange(members.Select(x => MapToResponse(x.Profile, x.Membership)));

        return ApiResponse<List<WorkerResponse>>.Create(responses, true, "Assignable workers retrieved", 200);
    }

    public async Task<ApiResponse<WorkerResponse>> CreateAsync(
        Guid userId,
        CreateWorkerRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validation = ValidateWorkerRequest(request.FirstName, request.LastName, request.Role);
        if (validation is not null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, validation, 400);
        }

        var profile = new AppUserProfile
        {
            Id = Guid.NewGuid(),
            AuthUserId = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            IsActive = true,
        };

        var membership = new BusinessMembership
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            UserProfileId = profile.Id,
            Role = request.Role.Trim().ToLowerInvariant(),
            IsPrimary = false,
            IsActive = true,
        };

        dbContext.UserProfiles.Add(profile);
        dbContext.BusinessMemberships.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerResponse>.Create(MapToResponse(profile, membership), true, "Worker created", 201);
    }

    public async Task<ApiResponse<WorkerResponse>> UpdateAsync(
        Guid userId,
        Guid workerId,
        UpdateWorkerRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validation = ValidateWorkerRequest(request.FirstName, request.LastName, request.Role);
        if (validation is not null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, validation, 400);
        }

        var membership = await dbContext.BusinessMemberships
            .FirstOrDefaultAsync(m => m.UserProfileId == workerId && m.BusinessId == business.Id, cancellationToken);

        if (membership is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Worker not found", 404);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.Id == workerId, cancellationToken);

        if (profile is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Worker profile not found", 404);
        }

        profile.FirstName = request.FirstName.Trim();
        profile.LastName = request.LastName.Trim();
        profile.DisplayName = $"{request.FirstName.Trim()} {request.LastName.Trim()}";
        profile.Email = NormalizeOptional(request.Email);
        profile.Phone = NormalizeOptional(request.Phone);
        membership.Role = request.Role.Trim().ToLowerInvariant();

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerResponse>.Create(MapToResponse(profile, membership), true, "Worker updated", 200);
    }

    public async Task<ApiResponse<WorkerResponse>> DeactivateAsync(
        Guid userId,
        Guid workerId,
        CancellationToken cancellationToken = default)
    {
        var business = await GetBusinessAsync(userId, cancellationToken);
        if (business is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Business not found for user", 404);
        }

        var membership = await dbContext.BusinessMemberships
            .FirstOrDefaultAsync(m => m.UserProfileId == workerId && m.BusinessId == business.Id, cancellationToken);

        if (membership is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Worker not found", 404);
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.Id == workerId, cancellationToken);

        if (profile is null)
        {
            return ApiResponse<WorkerResponse>.Create(null, false, "Worker profile not found", 404);
        }

        profile.IsActive = false;
        membership.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<WorkerResponse>.Create(MapToResponse(profile, membership), true, "Worker deactivated", 200);
    }

    private async Task<Domain.Entities.Business?> GetBusinessAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.AuthUserId == userId, cancellationToken);
    }

    private static WorkerResponse MapToResponse(AppUserProfile profile, BusinessMembership membership) =>
        new(
            profile.Id,
            membership.Id,
            BuildDisplayName(profile),
            profile.FirstName,
            profile.LastName,
            profile.Email,
            profile.Phone,
            membership.Role,
            profile.IsActive,
            profile.CreatedAt);

    private static string BuildDisplayName(AppUserProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            return profile.DisplayName;
        }

        return $"{profile.FirstName} {profile.LastName}".Trim();
    }

    private static string? ValidateWorkerRequest(string firstName, string lastName, string role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "First name is required";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "Last name is required";
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return "Role is required";
        }

        if (!ManagedRoles.Contains(role.Trim()))
        {
            return $"Role must be one of: {string.Join(", ", ManagedRoles)}";
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
