using Fieldore.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Fieldore.Infrastructure.Data;

internal static class LocationSeedData
{
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 3, 29, 0, 0, 0, TimeSpan.Zero);

    internal static readonly Country UnitedStates = CreateCountry("United States", "US");
    internal static readonly Country Canada = CreateCountry("Canada", "CA");
    internal static readonly Country UnitedKingdom = CreateCountry("United Kingdom", "GB");
    internal static readonly Country Ireland = CreateCountry("Ireland", "IE");
    internal static readonly Country Malta = CreateCountry("Malta", "MT");
    internal static readonly Country Australia = CreateCountry("Australia", "AU");
    internal static readonly Country NewZealand = CreateCountry("New Zealand", "NZ");

    internal static IReadOnlyList<Country> Countries { get; } =
    [
        UnitedStates,
        Canada,
        UnitedKingdom,
        Ireland,
        Malta,
        Australia,
        NewZealand
    ];

    internal static IReadOnlyList<StateProvince> States { get; } =
    [
        .. BuildStates(UnitedStates,
            ("Alabama", "AL"), ("Alaska", "AK"), ("Arizona", "AZ"), ("Arkansas", "AR"),
            ("California", "CA"), ("Colorado", "CO"), ("Connecticut", "CT"), ("Delaware", "DE"),
            ("Florida", "FL"), ("Georgia", "GA"), ("Hawaii", "HI"), ("Idaho", "ID"),
            ("Illinois", "IL"), ("Indiana", "IN"), ("Iowa", "IA"), ("Kansas", "KS"),
            ("Kentucky", "KY"), ("Louisiana", "LA"), ("Maine", "ME"), ("Maryland", "MD"),
            ("Massachusetts", "MA"), ("Michigan", "MI"), ("Minnesota", "MN"), ("Mississippi", "MS"),
            ("Missouri", "MO"), ("Montana", "MT"), ("Nebraska", "NE"), ("Nevada", "NV"),
            ("New Hampshire", "NH"), ("New Jersey", "NJ"), ("New Mexico", "NM"), ("New York", "NY"),
            ("North Carolina", "NC"), ("North Dakota", "ND"), ("Ohio", "OH"), ("Oklahoma", "OK"),
            ("Oregon", "OR"), ("Pennsylvania", "PA"), ("Rhode Island", "RI"), ("South Carolina", "SC"),
            ("South Dakota", "SD"), ("Tennessee", "TN"), ("Texas", "TX"), ("Utah", "UT"),
            ("Vermont", "VT"), ("Virginia", "VA"), ("Washington", "WA"), ("West Virginia", "WV"),
            ("Wisconsin", "WI"), ("Wyoming", "WY"), ("District of Columbia", "DC")),
        .. BuildStates(Canada,
            ("Alberta", "AB"), ("British Columbia", "BC"), ("Manitoba", "MB"), ("New Brunswick", "NB"),
            ("Newfoundland and Labrador", "NL"), ("Nova Scotia", "NS"), ("Ontario", "ON"),
            ("Prince Edward Island", "PE"), ("Quebec", "QC"), ("Saskatchewan", "SK"),
            ("Northwest Territories", "NT"), ("Nunavut", "NU"), ("Yukon", "YT")),
        .. BuildStates(UnitedKingdom,
            ("England", "ENG"), ("Scotland", "SCT"), ("Wales", "WLS"), ("Northern Ireland", "NIR")),
        .. BuildStates(Ireland,
            ("Leinster", "L"), ("Munster", "M"), ("Connacht", "C"), ("Ulster", "U")),
        .. BuildStates(Malta,
            ("Northern Region", "NR"), ("Southern Region", "SR"), ("South Eastern Region", "SER"),
            ("Western Region", "WR"), ("Gozo Region", "GR")),
        .. BuildStates(Australia,
            ("New South Wales", "NSW"), ("Victoria", "VIC"), ("Queensland", "QLD"),
            ("Western Australia", "WA"), ("South Australia", "SA"), ("Tasmania", "TAS"),
            ("Australian Capital Territory", "ACT"), ("Northern Territory", "NT")),
        .. BuildStates(NewZealand,
            ("Northland", "NTL"), ("Auckland", "AUK"), ("Waikato", "WKO"), ("Bay of Plenty", "BOP"),
            ("Gisborne", "GIS"), ("Hawke's Bay", "HKB"), ("Taranaki", "TKI"), ("Manawatu-Whanganui", "MWT"),
            ("Wellington", "WGN"), ("Tasman", "TAS"), ("Nelson", "NSN"), ("Marlborough", "MBH"),
            ("West Coast", "WTC"), ("Canterbury", "CAN"), ("Otago", "OTA"), ("Southland", "STL"),
            ("Chatham Islands", "CIT"))
    ];

    private static Country CreateCountry(string name, string code)
    {
        return new Country
        {
            Id = CreateGuid($"country:{code}"),
            Name = name,
            Code = code,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        };
    }

    private static IEnumerable<StateProvince> BuildStates(Country country, params (string Name, string Code)[] states)
    {
        foreach (var state in states)
        {
            yield return new StateProvince
            {
                Id = CreateGuid($"state:{country.Code}:{state.Code}"),
                CountryId = country.Id,
                Name = state.Name,
                Code = state.Code,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            };
        }
    }

    private static Guid CreateGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
