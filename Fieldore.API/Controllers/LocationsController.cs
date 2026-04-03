using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Locations.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class LocationsController(ILocationLookupService locationLookupService) : ControllerBase
{
    [HttpGet("countries")]
    public async Task<ActionResult<ApiResponse<List<CountryLookupResponse>>>> GetCountries(
        CancellationToken cancellationToken)
    {
        return await locationLookupService.GetCountriesAsync(cancellationToken);
    }

    [HttpGet("states")]
    public async Task<ActionResult<ApiResponse<List<StateProvinceLookupResponse>>>> GetStates(
        [FromQuery] Guid? countryId,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        return await locationLookupService.GetStatesAsync(countryId, countryCode, cancellationToken);
    }
}
