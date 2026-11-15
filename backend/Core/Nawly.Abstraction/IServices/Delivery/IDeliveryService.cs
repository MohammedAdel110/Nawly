using Nawly.Shared.Common.Results;
using Nawly.Shared.DTOs.Admin;
using Nawly.Shared.DTOs.Delivery;

namespace Nawly.Abstraction.IServices.Delivery;

public interface IDeliveryService
{
    Task<Result<DeliveryDto>> CreateForOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<Result<int>> DispatchWaveAsync(Guid deliveryId, int wave, CancellationToken ct = default);
    Task<Result<IReadOnlyList<DeliveryOfferDto>>> GetPendingOffersForCourierAsync(Guid courierUserId, CancellationToken ct = default);
    Task<Result<DeliveryDto>> AcceptOfferAsync(Guid courierUserId, Guid offerId, CancellationToken ct = default);
    Task<Result> DeclineOfferAsync(Guid courierUserId, Guid offerId, CancellationToken ct = default);
    Task<Result<DeliveryDto?>> GetActiveDeliveryForCourierAsync(Guid courierUserId, CancellationToken ct = default);
    Task<Result<DeliveryDto>> MarkArrivedAtStoreAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default);
    Task<Result<DeliveryDto>> MarkPickedUpAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default);
    Task<Result<DeliveryDto>> MarkArrivedAtCustomerAsync(Guid courierUserId, Guid deliveryId, CancellationToken ct = default);
    Task<Result<DeliveryDto>> MarkDeliveredAsync(Guid courierUserId, Guid deliveryId, ConfirmDeliveryCodeDto request, CancellationToken ct = default);
    Task<Result<DeliveryDto>> MarkFailedAsync(Guid courierUserId, Guid deliveryId, FailDeliveryDto request, CancellationToken ct = default);
    Task<Result<DeliveryDto>> AssignCourierManuallyAsync(Guid adminUserId, Guid deliveryId, AssignCourierRequestDto request, CancellationToken ct = default);
}
