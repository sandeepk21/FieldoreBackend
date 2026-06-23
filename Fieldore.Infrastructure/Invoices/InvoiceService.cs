using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Invoices.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Invoices;

public sealed class InvoiceService(FieldoreDbContext dbContext) : IInvoiceService
{
    public async Task<ApiResponse<InvoiceResponse>> CreateAsync(
        Guid userId,
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateInvoiceRequest(
            request.Status,
            request.IssuedOn,
            request.DueOn,
            request.TaxRate,
            request.DiscountAmount,
            request.LineItems);

        if (validationMessage is not null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, validationMessage, 400);
        }

        var customer = await GetCustomerAsync(businessId.Value, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Customer not found", 404);
        }

        if (request.JobId.HasValue)
        {
            var jobValidation = await ValidateJobOwnershipAsync(
                businessId.Value,
                request.JobId.Value,
                request.CustomerId,
                cancellationToken);

            if (jobValidation is not null)
            {
                return ApiResponse<InvoiceResponse>.Create(null, false, jobValidation, 400);
            }
        }

        var totals = CalculateTotals(request.LineItems, request.TaxRate, request.DiscountAmount, 0m);
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            CustomerId = request.CustomerId,
            JobId = request.JobId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(businessId.Value, cancellationToken),
            PurchaseOrderNumber = NormalizeOptional(request.PurchaseOrderNumber),
            NetTerms = NormalizeNetTerms(request.NetTerms),
            Status = NormalizeStatus(request.Status),
            IssuedOn = request.IssuedOn,
            DueOn = request.DueOn,
            TaxRate = NormalizeCurrency(request.TaxRate),
            DiscountAmount = totals.DiscountAmount,
            SubtotalAmount = totals.SubtotalAmount,
            TaxAmount = totals.TaxAmount,
            TotalAmount = totals.TotalAmount,
            BalanceDueAmount = totals.BalanceDueAmount,
            Notes = NormalizeOptional(request.Notes),
            CustomerNameSnapshot = BuildCustomerDisplayName(customer),
            CustomerEmailSnapshot = customer.Email,
            BillingAddressSnapshot = BuildBillingAddressSnapshot(customer, request.BillingAddress),
            LineItems = BuildLineItems(request.LineItems)
        };

        foreach (var lineItem in invoice.LineItems)
        {
            lineItem.InvoiceId = invoice.Id;
        }

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, invoice.Id, cancellationToken);
    }

    public async Task<ApiResponse<PagedResponse<InvoiceResponse>>> GetAllAsync(
        Guid userId,
        GetInvoicesRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<PagedResponse<InvoiceResponse>>.Create(null, false, "Business not found for user", 404);
        }

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var query = dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value);

        if (request.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);
        }

        if (request.JobId.HasValue)
        {
            query = query.Where(x => x.JobId == request.JobId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            query = query.Where(x => x.Status == status);
        }

        if (request.IssuedFrom.HasValue)
        {
            query = query.Where(x => x.IssuedOn >= request.IssuedFrom.Value);
        }

        if (request.IssuedTo.HasValue)
        {
            query = query.Where(x => x.IssuedOn <= request.IssuedTo.Value);
        }

        if (request.DueFrom.HasValue)
        {
            query = query.Where(x => x.DueOn >= request.DueFrom.Value);
        }

        if (request.DueTo.HasValue)
        {
            query = query.Where(x => x.DueOn <= request.DueTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.InvoiceNumber.ToLower().Contains(search) ||
                (x.PurchaseOrderNumber != null && x.PurchaseOrderNumber.ToLower().Contains(search)) ||
                x.CustomerNameSnapshot.ToLower().Contains(search) ||
                (x.CustomerEmailSnapshot != null && x.CustomerEmailSnapshot.ToLower().Contains(search)));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responses = await BuildInvoiceResponsesAsync(invoices, cancellationToken);
        var pagedResponse = new PagedResponse<InvoiceResponse>
        {
            Data = responses,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return ApiResponse<PagedResponse<InvoiceResponse>>.Create(
            pagedResponse, true, "Invoices fetched successfully", 200);
    }

    public async Task<ApiResponse<InvoiceResponse>> GetByIdAsync(
        Guid userId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Business not found for user", 404);
        }

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);

        if (invoice is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Invoice not found", 404);
        }

        var response = (await BuildInvoiceResponsesAsync([invoice], cancellationToken)).Single();
        return ApiResponse<InvoiceResponse>.Create(response, true, "Invoice fetched successfully", 200);
    }

    public async Task<ApiResponse<InvoiceResponse>> UpdateAsync(
        Guid userId,
        Guid invoiceId,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateInvoiceRequest(
            request.Status,
            request.IssuedOn,
            request.DueOn,
            request.TaxRate,
            request.DiscountAmount,
            request.LineItems);

        if (validationMessage is not null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, validationMessage, 400);
        }

        var invoice = await dbContext.Invoices
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);

        if (invoice is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Invoice not found", 404);
        }

        var customer = await GetCustomerAsync(businessId.Value, request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Customer not found", 404);
        }

        if (request.JobId.HasValue)
        {
            var jobValidation = await ValidateJobOwnershipAsync(
                businessId.Value,
                request.JobId.Value,
                request.CustomerId,
                cancellationToken);

            if (jobValidation is not null)
            {
                return ApiResponse<InvoiceResponse>.Create(null, false, jobValidation, 400);
            }
        }

        var paidAmount = await GetRecordedPaymentsTotalAsync(invoice.Id, cancellationToken);
        var totals = CalculateTotals(request.LineItems, request.TaxRate, request.DiscountAmount, paidAmount);

        invoice.CustomerId = request.CustomerId;
        invoice.JobId = request.JobId;
        invoice.PurchaseOrderNumber = NormalizeOptional(request.PurchaseOrderNumber);
        invoice.NetTerms = NormalizeNetTerms(request.NetTerms);
        invoice.Status = NormalizeStatus(request.Status);
        invoice.IssuedOn = request.IssuedOn;
        invoice.DueOn = request.DueOn;
        invoice.TaxRate = NormalizeCurrency(request.TaxRate);
        invoice.DiscountAmount = totals.DiscountAmount;
        invoice.SubtotalAmount = totals.SubtotalAmount;
        invoice.TaxAmount = totals.TaxAmount;
        invoice.TotalAmount = totals.TotalAmount;
        invoice.BalanceDueAmount = totals.BalanceDueAmount;
        invoice.Notes = NormalizeOptional(request.Notes);
        invoice.CustomerNameSnapshot = BuildCustomerDisplayName(customer);
        invoice.CustomerEmailSnapshot = customer.Email;
        invoice.BillingAddressSnapshot = BuildBillingAddressSnapshot(customer, request.BillingAddress);

        await dbContext.InvoiceLineItems
            .Where(x => x.InvoiceId == invoice.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var newLineItems = BuildLineItems(request.LineItems);
        foreach (var lineItem in newLineItems)
        {
            lineItem.InvoiceId = invoice.Id;
        }

        await dbContext.InvoiceLineItems.AddRangeAsync(newLineItems, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, invoice.Id, cancellationToken);
    }

    public async Task<ApiResponse<InvoiceResponse>> UpdateStatusAsync(
        Guid userId,
        Guid invoiceId,
        UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Business not found for user", 404);
        }

        if (!IsValidStatus(request.Status))
        {
            return ApiResponse<InvoiceResponse>.Create(
                null, false, "Invoice status must be draft, sent, viewed, partially_paid, paid, overdue, or void", 400);
        }

        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);

        if (invoice is null)
        {
            return ApiResponse<InvoiceResponse>.Create(null, false, "Invoice not found", 404);
        }

        invoice.Status = NormalizeStatus(request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(userId, invoiceId, cancellationToken);
    }

    public async Task<ApiResponse<DeleteInvoiceResponse>> DeleteAsync(
        Guid userId,
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteInvoiceResponse>.Create(null, false, "Business not found for user", 404);
        }

        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);

        if (invoice is null)
        {
            return ApiResponse<DeleteInvoiceResponse>.Create(null, false, "Invoice not found", 404);
        }

        var hasPayments = await dbContext.PaymentRecords
            .AnyAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (hasPayments)
        {
            return ApiResponse<DeleteInvoiceResponse>.Create(
                null, false, "Invoice cannot be deleted because payment records exist", 409);
        }

        await dbContext.InvoiceLineItems
            .Where(x => x.InvoiceId == invoiceId)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.Invoices.Remove(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DeleteInvoiceResponse(invoiceId, "Invoice deleted successfully");
        return ApiResponse<DeleteInvoiceResponse>.Create(response, true, "Invoice deleted successfully", 200);
    }

    private async Task<List<InvoiceResponse>> BuildInvoiceResponsesAsync(
        List<Invoice> invoices,
        CancellationToken cancellationToken)
    {
        if (invoices.Count == 0)
        {
            return [];
        }

        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var customerIds = invoices.Select(x => x.CustomerId).Distinct().ToList();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var lineItems = await dbContext.InvoiceLineItems
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .ToListAsync(cancellationToken);

        var paymentRecords = await dbContext.PaymentRecords
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.InvoiceId))
            .OrderBy(x => x.PaidAt)
            .ToListAsync(cancellationToken);

        return invoices
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .Select(invoice =>
            {
                customers.TryGetValue(invoice.CustomerId, out var customer);

                var responseLineItems = lineItems
                    .Where(x => x.InvoiceId == invoice.Id)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.CreatedAt)
                    .Select(x => new InvoiceLineItemResponse(
                        x.Id,
                        x.SortOrder,
                        x.Name,
                        x.Description,
                        x.Quantity,
                        x.UnitRate,
                        x.LineTotal))
                    .ToList();

                var responsePayments = paymentRecords
                    .Where(x => x.InvoiceId == invoice.Id)
                    .Select(x => new PaymentRecordResponse(
                        x.Id, x.Amount, x.Method, x.PaidAt,
                        x.ReferenceNumber, x.Notes,
                        x.Method == "stripe",
                        x.CreatedAt))
                    .ToList();

                return new InvoiceResponse(
                    invoice.Id,
                    invoice.BusinessId,
                    invoice.CustomerId,
                    invoice.JobId,
                    invoice.InvoiceNumber,
                    invoice.PurchaseOrderNumber,
                    invoice.NetTerms,
                    invoice.Status,
                    invoice.IssuedOn,
                    invoice.DueOn,
                    invoice.TaxRate,
                    invoice.DiscountAmount,
                    invoice.SubtotalAmount,
                    invoice.TaxAmount,
                    invoice.TotalAmount,
                    invoice.BalanceDueAmount,
                    invoice.Notes,
                    invoice.CustomerNameSnapshot,
                    invoice.CustomerEmailSnapshot,
                    MapAddress(invoice.BillingAddressSnapshot),
                    customer is null
                        ? null
                        : new InvoiceCustomerSummaryResponse(
                            customer.Id,
                            BuildCustomerDisplayName(customer),
                            customer.Email,
                            customer.MobilePhone),
                    responseLineItems,
                    responsePayments,
                    invoice.CreatedAt,
                    invoice.UpdatedAt);
            })
            .ToList();
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Customer?> GetCustomerAsync(Guid businessId, Guid customerId, CancellationToken cancellationToken)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(
                x => x.Id == customerId && x.BusinessId == businessId && x.IsActive,
                cancellationToken);
    }

    private async Task<string?> ValidateJobOwnershipAsync(
        Guid businessId,
        Guid jobId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == jobId && x.BusinessId == businessId, cancellationToken);

        if (job is null)
        {
            return "Job not found";
        }

        if (job.CustomerId != customerId)
        {
            return "Selected job does not belong to the selected customer";
        }

        return null;
    }

    private async Task<decimal> GetRecordedPaymentsTotalAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        return await dbContext.PaymentRecords
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var existingNumbers = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .Select(x => x.InvoiceNumber)
            .ToListAsync(cancellationToken);

        var sequence = existingNumbers
            .Select(ParseInvoiceSequence)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var invoiceNumber = FormatInvoiceNumber(sequence);

        while (existingNumbers.Contains(invoiceNumber, StringComparer.OrdinalIgnoreCase))
        {
            sequence++;
            invoiceNumber = FormatInvoiceNumber(sequence);
        }

        return invoiceNumber;
    }

    private static string? ValidateInvoiceRequest(
        string status,
        DateOnly issuedOn,
        DateOnly dueOn,
        decimal taxRate,
        decimal discountAmount,
        List<InvoiceLineItemRequest>? lineItems)
    {
        if (!IsValidStatus(status))
        {
            return "Invoice status must be draft, sent, viewed, partially_paid, paid, overdue, or void";
        }

        if (dueOn < issuedOn)
        {
            return "Due date must be on or after issue date";
        }

        if (taxRate < 0)
        {
            return "Tax rate cannot be negative";
        }

        if (discountAmount < 0)
        {
            return "Discount amount cannot be negative";
        }

        if (lineItems is null || lineItems.Count == 0)
        {
            return "At least one invoice line item is required";
        }

        if (lineItems.Any(x => string.IsNullOrWhiteSpace(x.Name)))
        {
            return "Invoice line item name is required";
        }

        if (lineItems.Any(x => x.Quantity <= 0))
        {
            return "Invoice line item quantity must be greater than zero";
        }

        if (lineItems.Any(x => x.UnitRate < 0))
        {
            return "Invoice line item unit rate cannot be negative";
        }

        return null;
    }

    private static List<InvoiceLineItem> BuildLineItems(List<InvoiceLineItemRequest>? lineItems)
    {
        return lineItems?
            .Select(x =>
            {
                var quantity = NormalizeCurrency(x.Quantity);
                var unitRate = NormalizeCurrency(x.UnitRate);

                return new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    SortOrder = x.SortOrder,
                    Name = x.Name.Trim(),
                    Description = NormalizeOptional(x.Description),
                    Quantity = quantity,
                    UnitRate = unitRate,
                    LineTotal = NormalizeCurrency(quantity * unitRate)
                };
            })
            .ToList() ?? [];
    }

    private static InvoiceTotals CalculateTotals(
        List<InvoiceLineItemRequest>? lineItems,
        decimal taxRate,
        decimal discountAmount,
        decimal paidAmount)
    {
        var normalizedTaxRate = NormalizeCurrency(taxRate);
        var normalizedDiscount = NormalizeCurrency(discountAmount);
        var subtotal = NormalizeCurrency(lineItems?.Sum(x => NormalizeCurrency(x.Quantity) * NormalizeCurrency(x.UnitRate)) ?? 0m);
        var taxableAmount = Math.Max(0m, subtotal - normalizedDiscount);
        var taxAmount = NormalizeCurrency(taxableAmount * normalizedTaxRate / 100m);
        var totalAmount = NormalizeCurrency(taxableAmount + taxAmount);
        var balanceDueAmount = NormalizeCurrency(Math.Max(0m, totalAmount - paidAmount));

        return new InvoiceTotals(
            subtotal,
            normalizedDiscount,
            taxAmount,
            totalAmount,
            balanceDueAmount);
    }

    private static Address? BuildBillingAddressSnapshot(Customer customer, InvoiceAddressRequest? request)
    {
        if (request is not null && HasAddressData(request))
        {
            return new Address
            {
                Line1 = NormalizeOptional(request.Line1),
                Line2 = NormalizeOptional(request.Line2),
                City = NormalizeOptional(request.City),
                StateOrProvince = NormalizeOptional(request.StateOrProvince),
                PostalCode = NormalizeOptional(request.PostalCode),
                Country = NormalizeOptional(request.Country),
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };
        }

        var address = customer.Addresses
            .OrderByDescending(x => x.IsBilling)
            .ThenByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.Address)
            .FirstOrDefault();

        return address is null
            ? null
            : new Address
            {
                Line1 = address.Line1,
                Line2 = address.Line2,
                City = address.City,
                StateOrProvince = address.StateOrProvince,
                PostalCode = address.PostalCode,
                Country = address.Country,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };
    }

    private static bool HasAddressData(InvoiceAddressRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Line1) ||
               !string.IsNullOrWhiteSpace(request.Line2) ||
               !string.IsNullOrWhiteSpace(request.City) ||
               !string.IsNullOrWhiteSpace(request.StateOrProvince) ||
               !string.IsNullOrWhiteSpace(request.PostalCode) ||
               !string.IsNullOrWhiteSpace(request.Country) ||
               request.Latitude.HasValue ||
               request.Longitude.HasValue;
    }

    private static InvoiceAddressResponse? MapAddress(Address? address)
    {
        return address is null
            ? null
            : new InvoiceAddressResponse(
                address.Line1,
                address.Line2,
                address.City,
                address.StateOrProvince,
                address.PostalCode,
                address.Country,
                address.Latitude,
                address.Longitude);
    }

    private static string BuildCustomerDisplayName(Customer customer)
    {
        var fullName = $"{customer.FirstName} {customer.LastName}".Trim();
        return string.IsNullOrWhiteSpace(customer.CompanyName)
            ? fullName
            : $"{customer.CompanyName.Trim()} - {fullName}";
    }

    private static bool IsValidStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is InvoiceStatuses.Draft
            or InvoiceStatuses.Sent
            or InvoiceStatuses.Viewed
            or InvoiceStatuses.PartiallyPaid
            or InvoiceStatuses.Paid
            or InvoiceStatuses.Overdue
            or InvoiceStatuses.Void
            or InvoiceStatuses.Unpaid;
    }

    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? InvoiceStatuses.Draft
            : status.Trim().ToLowerInvariant();
    }

    private static string NormalizeNetTerms(string? netTerms)
    {
        return string.IsNullOrWhiteSpace(netTerms) ? "Net 30" : netTerms.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static decimal NormalizeCurrency(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static int ParseInvoiceSequence(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return 0;
        }

        var parts = invoiceNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lastPart = parts.LastOrDefault();
        return int.TryParse(lastPart, out var sequence) ? sequence : 0;
    }

    private static string FormatInvoiceNumber(int sequence) => $"INV-{sequence:D6}";

    private sealed record InvoiceTotals(
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal BalanceDueAmount);
}
