namespace Fieldore.Application.Models;

public class BusinessDetailsResponse
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? TradeType { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public AddressDto? Address { get; set; }

    public string? TimeZone { get; set; }

    public string? Currency { get; set; }
}

public class AddressDto
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}