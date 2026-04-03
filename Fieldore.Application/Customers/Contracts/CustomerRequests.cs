namespace Fieldore.Application.Customers.Contracts;

public sealed record CustomerAddressRequest(
    string Label,
    bool IsPrimary,
    bool IsBilling,
    string Line1,
    string? Line2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country);

public sealed record CreateCustomerRequest(
    string Type,
    string? CompanyName,
    string FirstName,
    string LastName,
    string? Email,
    string MobilePhone,
    string? AlternatePhone,
    string? GateCode,
    string? PetsNote,
    string? InternalNotes,
    bool BillingSameAsService,
    List<CustomerAddressRequest>? Addresses);

public sealed record UpdateCustomerRequest(
    string Type,
    string? CompanyName,
    string FirstName,
    string LastName,
    string? Email,
    string MobilePhone,
    string? AlternatePhone,
    string? GateCode,
    string? PetsNote,
    string? InternalNotes,
    bool BillingSameAsService,
    List<CustomerAddressRequest>? Addresses);
