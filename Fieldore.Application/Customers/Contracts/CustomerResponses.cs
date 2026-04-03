namespace Fieldore.Application.Customers.Contracts;

public sealed record CustomerAddressResponse(
    Guid Id,
    string Label,
    bool IsPrimary,
    bool IsBilling,
    string Line1,
    string? Line2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country);

public sealed record CustomerResponse(
    Guid Id,
    Guid BusinessId,
    string Type,
    string? CompanyName,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    string MobilePhone,
    string? AlternatePhone,
    string? GateCode,
    string? PetsNote,
    string? InternalNotes,
    bool BillingSameAsService,
    bool IsActive,
    List<CustomerAddressResponse> Addresses,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
public class GetCustomersRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Type { get; set; } // residential/commercial
    public bool? IsActive { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
}

public sealed record DeleteCustomerResponse(Guid CustomerId, string Message);
