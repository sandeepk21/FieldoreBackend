using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Locations.Contracts;

public interface ILocationLookupService
{
    Task<ApiResponse<List<CountryLookupResponse>>> GetCountriesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<StateProvinceLookupResponse>>> GetStatesAsync(
        Guid? countryId,
        string? countryCode,
        CancellationToken cancellationToken = default);
}
