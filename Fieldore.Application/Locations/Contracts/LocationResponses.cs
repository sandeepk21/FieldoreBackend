namespace Fieldore.Application.Locations.Contracts;

public sealed record CountryLookupResponse(
    Guid Id,
    string Name,
    string Code);

public sealed record StateProvinceLookupResponse(
    Guid Id,
    Guid CountryId,
    string Name,
    string Code);
