using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Payments.Contracts;

public interface IPaymentService
{
    Task<ApiResponse<PaymentResponse>> RecordAsync(Guid userId, Guid invoiceId, RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<List<PaymentResponse>>> GetByInvoiceAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> DeleteAsync(Guid userId, Guid invoiceId, Guid paymentId, CancellationToken cancellationToken = default);
}
