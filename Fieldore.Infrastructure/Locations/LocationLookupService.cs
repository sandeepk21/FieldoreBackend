using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Locations.Contracts;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Locations;

public sealed class LocationLookupService(FieldoreDbContext dbContext) : ILocationLookupService
{
    public async Task<ApiResponse<List<CountryLookupResponse>>> GetCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        var countries = await dbContext.Countries
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CountryLookupResponse(x.Id, x.Name, x.Code))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<CountryLookupResponse>>.Create(
            countries, true, "Countries fetched successfully", 200);
    }

    public async Task<ApiResponse<List<StateProvinceLookupResponse>>> GetStatesAsync(
        Guid? countryId,
        string? countryCode,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StateProvinces
            .AsNoTracking()
            .AsQueryable();

        if (countryId.HasValue)
        {
            query = query.Where(x => x.CountryId == countryId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var normalizedCode = NormalizeCountryCode(countryCode);

            query = query.Where(x => x.Country!.Code == normalizedCode);
        }
        else
        {
            return ApiResponse<List<StateProvinceLookupResponse>>.Create(
                null, false, "countryId or countryCode is required", 400);
        }

        var states = await query
            .OrderBy(x => x.Name)
            .Select(x => new StateProvinceLookupResponse(x.Id, x.CountryId, x.Name, x.Code))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<StateProvinceLookupResponse>>.Create(
            states, true, "States fetched successfully", 200);
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        var normalizedCode = countryCode.Trim().ToUpperInvariant();

        return normalizedCode switch
        {
            "UK" => "GB",
            _ => normalizedCode
        };
    }
}
