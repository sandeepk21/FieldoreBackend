using Fieldore.Application.Auth.Contracts;
using Fieldore.Application.Payments.Contracts;
using Fieldore.Domain.Constants;
using Fieldore.Domain.Entities;
using Fieldore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.Infrastructure.Payments;

public sealed class PaymentService(FieldoreDbContext dbContext) : IPaymentService
{
    private static readonly HashSet<string> ValidMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        PaymentMethods.Cash, PaymentMethods.Card, PaymentMethods.BankTransfer,
        PaymentMethods.Check, PaymentMethods.Other
    };

    public async Task<ApiResponse<PaymentResponse>> RecordAsync(
        Guid userId, Guid invoiceId, RecordPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
            return ApiResponse<PaymentResponse>.Create(null, false, "Business not found for user", 404);

        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);
        if (invoice is null)
            return ApiResponse<PaymentResponse>.Create(null, false, "Invoice not found", 404);

        var validation = Validate(request.Amount, request.Method);
        if (validation is not null)
            return ApiResponse<PaymentResponse>.Create(null, false, validation, 400);

        var payment = new PaymentRecord
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            Method = request.Method.Trim().ToLowerInvariant(),
            PaidAt = request.PaidAt,
            ReferenceNumber = string.IsNullOrWhiteSpace(request.ReferenceNumber) ? null : request.ReferenceNumber.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RecordedByUserId = userId,
        };

        dbContext.PaymentRecords.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateInvoiceAsync(invoice, cancellationToken);

        return ApiResponse<PaymentResponse>.Create(MapToResponse(payment), true, "Payment recorded", 201);
    }

    public async Task<ApiResponse<List<PaymentResponse>>> GetByInvoiceAsync(
        Guid userId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
            return ApiResponse<List<PaymentResponse>>.Create(null, false, "Business not found for user", 404);

        var invoiceExists = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);
        if (!invoiceExists)
            return ApiResponse<List<PaymentResponse>>.Create(null, false, "Invoice not found", 404);

        var payments = await dbContext.PaymentRecords
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.PaidAt)
            .ToListAsync(cancellationToken);

        return ApiResponse<List<PaymentResponse>>.Create(payments.Select(MapToResponse).ToList(), true, "Payments retrieved", 200);
    }

    public async Task<ApiResponse<PaymentResponse>> DeleteAsync(
        Guid userId, Guid invoiceId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        var businessId = await GetBusinessIdAsync(userId, cancellationToken);
        if (businessId is null)
            return ApiResponse<PaymentResponse>.Create(null, false, "Business not found for user", 404);

        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId && x.BusinessId == businessId.Value, cancellationToken);
        if (invoice is null)
            return ApiResponse<PaymentResponse>.Create(null, false, "Invoice not found", 404);

        var payment = await dbContext.PaymentRecords
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.InvoiceId == invoiceId, cancellationToken);
        if (payment is null)
            return ApiResponse<PaymentResponse>.Create(null, false, "Payment record not found", 404);

        var snapshot = MapToResponse(payment);
        dbContext.PaymentRecords.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateInvoiceAsync(invoice, cancellationToken);

        return ApiResponse<PaymentResponse>.Create(snapshot, true, "Payment deleted", 200);
    }

    private async Task RecalculateInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var totalPaid = await dbContext.PaymentRecords
            .Where(x => x.InvoiceId == invoice.Id)
            .SumAsync(x => x.Amount, cancellationToken);

        var newBalance = Math.Max(0m, invoice.TotalAmount - totalPaid);
        invoice.BalanceDueAmount = newBalance;

        if (newBalance <= 0m)
            invoice.Status = "paid";
        else if (totalPaid > 0m)
            invoice.Status = "partially_paid";
        else if (invoice.Status is "paid" or "partially_paid")
            invoice.Status = "sent";

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> GetBusinessIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Businesses
            .Where(x => x.AuthUserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static PaymentResponse MapToResponse(PaymentRecord p) =>
        new(p.Id, p.InvoiceId, p.Amount, p.Method, p.PaidAt,
            p.ReferenceNumber, p.Notes, p.Method == "stripe", p.CreatedAt);

    private static string? Validate(decimal amount, string method)
    {
        if (amount <= 0) return "Amount must be greater than zero";
        if (string.IsNullOrWhiteSpace(method)) return "Payment method is required";
        if (!ValidMethods.Contains(method.Trim())) return $"Method must be one of: {string.Join(", ", ValidMethods)}";
        return null;
    }
}
