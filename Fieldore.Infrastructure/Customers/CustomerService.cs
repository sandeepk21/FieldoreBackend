using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Customers.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Domain.ValueObjects;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Customers;

public sealed class CustomerService(FieldoreDbContext dbContext) : ICustomerService
{
    public async Task<ApiResponse<CustomerResponse>> CreateAsync(
        Guid userId,
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateCustomerRequest(
            request.Type,
            request.FirstName,
            request.LastName,
            request.MobilePhone,
            request.Addresses);

        if (validationMessage is not null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, validationMessage, 400);
        }
        var isDuplicate = await IsDuplicateCustomerAsync(
            businessId.Value,
            request.MobilePhone.Trim(),
            NormalizeEmail(request.Email),
            null,
            cancellationToken);

        if (isDuplicate)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Customer with same mobile/email already exists", 409);
        }
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId.Value,
            Type = NormalizeCustomerType(request.Type),
            CompanyName = NormalizeOptional(request.CompanyName),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = NormalizeEmail(request.Email),
            MobilePhone = request.MobilePhone.Trim(),
            AlternatePhone = NormalizeOptional(request.AlternatePhone),
            GateCode = NormalizeOptional(request.GateCode),
            PetsNote = NormalizeOptional(request.PetsNote),
            InternalNotes = NormalizeOptional(request.InternalNotes),
            BillingSameAsService = request.BillingSameAsService,
            IsActive = true,
            Addresses = BuildAddresses(request.Addresses, customerId: null)
        };

        foreach (var address in customer.Addresses)
        {
            address.CustomerId = customer.Id;
        }

        dbContext.Customers.Add(customer);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "A customer with this mobile phone or email already exists", 409);
        }

        return ApiResponse<CustomerResponse>.Create(
            MapCustomerResponse(customer), true, "Customer created successfully", 201);
    }

    public async Task<ApiResponse<PagedResponse<CustomerResponse>>> GetAllAsync(
    Guid userId,
    GetCustomersRequest request,
    CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<PagedResponse<CustomerResponse>>.Create(
                null, false, "Business not found for user", 404);
        }

        var query = dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Addresses)
            .Where(x => x.BusinessId == businessId.Value);

        // ✅ Filtering
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.Trim().ToLower();
            query = query.Where(x => x.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(x => x.Addresses.Any(a => a.Address.City.ToLower().Contains(request.City.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            query = query.Where(x => x.Addresses.Any(a => a.Address.StateOrProvince == request.State));
        }

        // ✅ Searching (important)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.FirstName + " " + x.LastName).ToLower().Contains(search) ||
                x.MobilePhone.Contains(search) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.CompanyName != null && x.CompanyName.ToLower().Contains(search))
            );
        }

        // ✅ Total count BEFORE pagination
        var totalRecords = await query.CountAsync(cancellationToken);

        // ✅ Pagination
        var customers = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var responseData = customers
            .Select(customer => MapCustomerResponse(customer))
            .ToList();

        var pagedResponse = new PagedResponse<CustomerResponse>
        {
            Data = responseData,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return ApiResponse<PagedResponse<CustomerResponse>>.Create(
            pagedResponse, true, "Customers fetched successfully", 200);
    }

    public async Task<ApiResponse<CustomerResponse>> GetByIdAsync(
        Guid userId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Business not found for user", 404);
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(
                x => x.Id == customerId && x.BusinessId == businessId.Value && x.IsActive,
                cancellationToken);

        if (customer is null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Customer not found", 404);
        }

        var customerNotes = await dbContext.CustomerNotes
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var jobs = await dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value && x.CustomerId == customer.Id)
            .OrderByDescending(x => x.ScheduledStartAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value && x.CustomerId == customer.Id)
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var estimates = await dbContext.Estimates
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId.Value && x.CustomerId == customer.Id)
            .OrderByDescending(x => x.IssuedOn)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var noteAuthorIds = customerNotes
            .Where(x => x.CreatedByUserId.HasValue)
            .Select(x => x.CreatedByUserId!.Value)
            .Distinct()
            .ToList();

        var profiles = noteAuthorIds.Count == 0
            ? new Dictionary<Guid, AppUserProfile>()
            : await dbContext.UserProfiles
                .AsNoTracking()
                .Where(x => noteAuthorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        return ApiResponse<CustomerResponse>.Create(
            MapDetailedCustomerResponse(customer, customerNotes, jobs, invoices, estimates, profiles),
            true,
            "Customer fetched successfully",
            200);
    }

    public async Task<ApiResponse<CustomerResponse>> UpdateAsync(Guid userId, Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Business not found for user", 404);
        }

        var validationMessage = ValidateCustomerRequest(
            request.Type,
            request.FirstName,
            request.LastName,
            request.MobilePhone,
            request.Addresses);

        if (validationMessage is not null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, validationMessage, 400);
        }

        var customer = await dbContext.Customers
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(
                x => x.Id == customerId && x.BusinessId == businessId.Value && x.IsActive,
                cancellationToken);

        if (customer is null)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Customer not found", 404);
        }
        var isDuplicate = await IsDuplicateCustomerAsync(
            businessId.Value,
            request.MobilePhone.Trim(),
            NormalizeEmail(request.Email),
            customer.Id,
            cancellationToken);

        if (isDuplicate)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "Another customer with same mobile/email already exists", 409);
        }
        customer.Type = NormalizeCustomerType(request.Type);
        customer.CompanyName = NormalizeOptional(request.CompanyName);
        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = NormalizeEmail(request.Email);
        customer.MobilePhone = request.MobilePhone.Trim();
        customer.AlternatePhone = NormalizeOptional(request.AlternatePhone);
        customer.GateCode = NormalizeOptional(request.GateCode);
        customer.PetsNote = NormalizeOptional(request.PetsNote);
        customer.InternalNotes = NormalizeOptional(request.InternalNotes);
        customer.BillingSameAsService = request.BillingSameAsService;

        await dbContext.CustomerAddresses
            .Where(x => x.CustomerId == customer.Id)
            .ExecuteDeleteAsync(cancellationToken);
        var newAddresses = BuildAddresses(request.Addresses, customer.Id);

        await dbContext.CustomerAddresses.AddRangeAsync(newAddresses, cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return ApiResponse<CustomerResponse>.Create(
                null, false, "A customer with this mobile phone or email already exists", 409);
        }

        return ApiResponse<CustomerResponse>.Create(
            MapCustomerResponse(customer), true, "Customer updated successfully", 200);
    }

    public async Task<ApiResponse<DeleteCustomerResponse>> DeleteAsync(
        Guid userId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
        {
            return ApiResponse<DeleteCustomerResponse>.Create(
                null, false, "Business not found for user", 404);
        }

        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(
                x => x.Id == customerId && x.BusinessId == businessId.Value && x.IsActive,
                cancellationToken);

        if (customer is null)
        {
            return ApiResponse<DeleteCustomerResponse>.Create(
                null, false, "Customer not found", 404);
        }

        customer.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new DeleteCustomerResponse
        {
            CustomerId = customer.Id,
            Message = "Customer deleted successfully"
        };

        return ApiResponse<DeleteCustomerResponse>.Create(
            response, true, "Customer deleted successfully", 200);
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? ValidateCustomerRequest(
        string type,
        string firstName,
        string lastName,
        string mobilePhone,
        List<CustomerAddressRequest>? addresses)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(mobilePhone))
        {
            return "Type, first name, last name, and mobile phone are required";
        }

        var normalizedType = type.Trim().ToLowerInvariant();
        if (normalizedType != CustomerTypes.Residential && normalizedType != CustomerTypes.Commercial)
        {
            return "Customer type must be residential or commercial";
        }

        if (addresses is null || addresses.Count == 0)
        {
            return "At least one customer address is required";
        }

        if (addresses.Count(x => x.IsPrimary) != 1)
        {
            return "Exactly one primary address is required";
        }

        if (addresses.Any(x =>
                string.IsNullOrWhiteSpace(x.Label) ||
                string.IsNullOrWhiteSpace(x.Line1) ||
                string.IsNullOrWhiteSpace(x.City) ||
                string.IsNullOrWhiteSpace(x.StateOrProvince) ||
                string.IsNullOrWhiteSpace(x.PostalCode) ||
                string.IsNullOrWhiteSpace(x.Country)))
        {
            return "Each address must include label, line1, city, state, postal code, and country";
        }

        return null;
    }

    private static List<CustomerAddress> BuildAddresses(
        List<CustomerAddressRequest>? addresses,
        Guid? customerId)
    {
        return addresses?
            .Select(x => new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId ?? Guid.Empty,
                Label = x.Label.Trim(),
                IsPrimary = x.IsPrimary,
                IsBilling = x.IsBilling,
                Address = new Address
                {
                    Line1 = x.Line1.Trim(),
                    Line2 = NormalizeOptional(x.Line2),
                    City = x.City.Trim(),
                    StateOrProvince = x.StateOrProvince.Trim(),
                    PostalCode = x.PostalCode.Trim(),
                    Country = x.Country.Trim()
                }
            })
            .ToList() ?? [];
    }

    private static CustomerResponse MapCustomerResponse(
        Customer customer)
    {
        return MapDetailedCustomerResponse(
            customer,
            [],
            [],
            [],
            [],
            new Dictionary<Guid, AppUserProfile>());
    }

    private static CustomerResponse MapDetailedCustomerResponse(
        Customer customer,
        List<CustomerNote> customerNotes,
        List<Job> jobs,
        List<Invoice> invoices,
        List<Estimate> estimates,
        IReadOnlyDictionary<Guid, AppUserProfile> profiles)
    {
        var addresses = customer.Addresses
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Label)
            .Select(x => new CustomerAddressResponse
            {
                Id = x.Id,
                Label = x.Label,
                IsPrimary = x.IsPrimary,
                IsBilling = x.IsBilling,
                Line1 = x.Address.Line1,
                Line2 = x.Address.Line2,
                City = x.Address.City,
                StateOrProvince = x.Address.StateOrProvince,
                PostalCode = x.Address.PostalCode,
                Country = x.Address.Country
            })
            .ToList();

        var notes = customerNotes
            .Select(x =>
            {
                AppUserProfile? profile = null;
                if (x.CreatedByUserId.HasValue)
                {
                    profiles.TryGetValue(x.CreatedByUserId.Value, out profile);
                }

                return new CustomerNoteResponse
                {
                    Id = x.Id,
                    CreatedByUserId = x.CreatedByUserId,
                    Body = x.Body,
                    CreatedAt = x.CreatedAt,
                    CreatedByDisplayName = BuildProfileDisplayName(profile)
                };
            })
            .ToList();

        var jobResponses = jobs
            .Select(x => new CustomerJobSummaryResponse
            {
                Id = x.Id,
                JobNumber = x.JobNumber,
                Title = x.Title,
                JobType = x.JobType,
                Priority = x.Priority,
                Status = x.Status,
                ScheduledStartAt = x.ScheduledStartAt,
                ScheduledEndAt = x.ScheduledEndAt,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        var invoiceResponses = invoices
            .Select(x => new CustomerInvoiceSummaryResponse
            {
                Id = x.Id,
                JobId = x.JobId,
                InvoiceNumber = x.InvoiceNumber,
                Status = x.Status,
                IssuedOn = x.IssuedOn,
                DueOn = x.DueOn,
                TotalAmount = x.TotalAmount,
                BalanceDueAmount = x.BalanceDueAmount,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        var estimateResponses = estimates
            .Select(x => new CustomerEstimateSummaryResponse
            {
                Id = x.Id,
                EstimateNumber = x.EstimateNumber,
                Status = x.Status,
                IssuedOn = x.IssuedOn,
                ExpiresOn = x.ExpiresOn,
                TotalAmount = x.TotalAmount,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToList();

        return new CustomerResponse
        {
            Id = customer.Id,
            BusinessId = customer.BusinessId,
            Type = customer.Type,
            CompanyName = customer.CompanyName,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            DisplayName = BuildDisplayName(customer.CompanyName, customer.FirstName, customer.LastName),
            Email = customer.Email,
            MobilePhone = customer.MobilePhone,
            AlternatePhone = customer.AlternatePhone,
            GateCode = customer.GateCode,
            PetsNote = customer.PetsNote,
            InternalNotes = customer.InternalNotes,
            BillingSameAsService = customer.BillingSameAsService,
            IsActive = customer.IsActive,
            Addresses = addresses,
            Notes = notes,
            Jobs = jobResponses,
            Invoices = invoiceResponses,
            Estimates = estimateResponses,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }

    private static string BuildDisplayName(string? companyName, string firstName, string lastName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(companyName)
            ? fullName
            : $"{companyName.Trim()} - {fullName}";
    }

    private static string NormalizeCustomerType(string type)
    {
        return type.Trim().ToLowerInvariant();
    }

    private static string? BuildProfileDisplayName(AppUserProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            return profile.DisplayName.Trim();
        }

        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? profile.Email : fullName;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
    }
    private async Task<bool> IsDuplicateCustomerAsync(
        Guid businessId,
        string mobilePhone,
        string? email,
        Guid? excludeCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .AnyAsync(x =>
                    x.BusinessId == businessId &&
                    x.IsActive &&
                    (x.MobilePhone == mobilePhone || (email != null && x.Email == email)) &&
                    (excludeCustomerId == null || x.Id != excludeCustomerId),
                cancellationToken);
    }
}
