using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Models.Delivery;
using Nawly.Domain.Models.Identity;

namespace Nawly.Domain.Contracts.SpecificReposPattern;

public interface IDeliveryRepository : IGenericRepo<Delivery, Guid>
{
    /// <summary>
    /// Atomically claims a delivery for a courier when CourierId is still null (SRS §5.3).
    /// </summary>
    Task<bool> TryAssignCourierAtomicallyAsync(Guid deliveryId, Guid courierId, DateTimeOffset assignedAtUtc, CancellationToken cancellationToken = default);
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Delivery>> GetActiveByCourierIdAsync(Guid courierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourierProfile>> GetEligibleCouriersForDispatchAsync(DateTimeOffset locationUpdatedSinceUtc, CancellationToken cancellationToken = default);
}
