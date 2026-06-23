namespace Fieldore.Application.Customers.Contracts;

public class CustomerAddressResponse
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsBilling { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class CustomerNoteResponse
{
    public Guid Id { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByDisplayName { get; set; }
}

public class CustomerJobSummaryResponse
{
    public Guid Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? JobType { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ScheduledStartAt { get; set; }
    public DateTimeOffset? ScheduledEndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CustomerInvoiceSummaryResponse
{
    public Guid Id { get; set; }
    public Guid? JobId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueOn { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal BalanceDueAmount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CustomerEstimateSummaryResponse
{
    public Guid Id { get; set; }
    public string EstimateNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CustomerResponse
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MobilePhone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? GateCode { get; set; }
    public string? PetsNote { get; set; }
    public string? InternalNotes { get; set; }
    public bool BillingSameAsService { get; set; }
    public bool IsActive { get; set; }
    public List<CustomerAddressResponse> Addresses { get; set; } = [];
    public List<CustomerNoteResponse> Notes { get; set; } = [];
    public List<CustomerJobSummaryResponse> Jobs { get; set; } = [];
    public List<CustomerInvoiceSummaryResponse> Invoices { get; set; } = [];
    public List<CustomerEstimateSummaryResponse> Estimates { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
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

public class DeleteCustomerResponse
{
    public Guid CustomerId { get; set; }
    public string Message { get; set; } = string.Empty;
}
