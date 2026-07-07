using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Invoices.Contracts;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceResponse>> CreateAsync(
        Guid userId,
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<InvoiceResponse>>> GetAllAsync(
        Guid userId,
        GetInvoicesRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceResponse>> GetByIdAsync(
        Guid userId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceResponse>> UpdateAsync(
        Guid userId,
        Guid invoiceId,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceResponse>> UpdateStatusAsync(
        Guid userId,
        Guid invoiceId,
        UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<InvoiceResponse?>> GetByJobIdAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<DeleteInvoiceResponse>> DeleteAsync(
        Guid userId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SendInvoiceResponse>> SendInvoiceAsync(
        Guid userId,
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
