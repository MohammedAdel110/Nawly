using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Models.Ordering;

namespace Nawly.Domain.Contracts.SpecificReposPattern;

public interface IOrderRepository : IGenericRepo<Order, Guid>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdempotencyKeyAsync(Guid customerId, Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task<int> GetNextDailySequenceAsync(DateOnly cairoDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetExpiredUnacceptedOrdersAsync(DateTimeOffset placedBeforeUtc, CancellationToken cancellationToken = default);
}
