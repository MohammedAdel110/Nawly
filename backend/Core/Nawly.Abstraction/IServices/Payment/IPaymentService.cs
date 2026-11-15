using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Payment;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Payment;
using Nawly.Shared.EnumHelper.PaymentEnums;

namespace Nawly.Abstraction.IServices.Payment;

public interface IPaymentService
{
    Task<Result<InitiatePaymentResponseDto>> InitiateAsync(Guid userId, Guid orderId, Guid idempotencyKey, CancellationToken ct = default);
    Task<Result<WebhookProcessResultDto>> ProcessWebhookAsync(PaymentProvider provider, string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);
    Task<Result<PaginationResponse<PaymentDto>>> SearchPaymentsAsync(PaymentQueryParams queryParams, CancellationToken ct = default);
    Task<Result<PaymentDto>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Result<RefundDto>> RequestRefundAsync(Guid adminUserId, Guid paymentId, RefundRequestDto request, CancellationToken ct = default);
}
