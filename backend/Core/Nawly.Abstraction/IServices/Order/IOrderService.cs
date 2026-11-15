using Nawly.Shared.Common.Pagination;
using Nawly.Shared.Common.Params.Order;
using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Order;

namespace Nawly.Abstraction.IServices.Order;

public interface IOrderService
{
    Task<Result<OrderDetailsDto>> PlaceOrderAsync(Guid userId, PlaceOrderRequestDto request, Guid idempotencyKey, CancellationToken ct = default);
    Task<Result<PaginationResponse<OrderSummaryDto>>> GetCustomerOrdersAsync(Guid customerId, OrderQueryParams queryParams, CancellationToken ct = default);
    Task<Result<PaginationResponse<OrderSummaryDto>>> GetStoreOrdersAsync(Guid storeId, OrderQueryParams queryParams, CancellationToken ct = default);
    Task<Result<PaginationResponse<OrderSummaryDto>>> SearchOrdersAsync(OrderQueryParams queryParams, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> GetOrderDetailsAsync(Guid orderId, Guid actorUserId, IReadOnlyCollection<string> actorRoles, Guid? actorStoreId, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> AcceptOrderAsync(Guid storeId, Guid orderId, Guid actorUserId, AcceptOrderDto request, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> RejectOrderAsync(Guid storeId, Guid orderId, Guid actorUserId, RejectOrderDto request, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> MarkPreparingAsync(Guid storeId, Guid orderId, Guid actorUserId, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> MarkReadyForPickupAsync(Guid storeId, Guid orderId, Guid actorUserId, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> CancelByCustomerAsync(Guid customerId, Guid orderId, CancelOrderDto request, CancellationToken ct = default);
    Task<Result<OrderDetailsDto>> AdminChangeStatusAsync(Guid adminUserId, Guid orderId, AdminChangeOrderStatusDto request, CancellationToken ct = default);
    Task<int> AutoRejectExpiredOrdersAsync(TimeSpan timeout, CancellationToken ct = default);
}
