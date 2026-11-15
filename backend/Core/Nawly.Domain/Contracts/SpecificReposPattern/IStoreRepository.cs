using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Models.Catalog;

namespace Nawly.Domain.Contracts.SpecificReposPattern;

public interface IStoreRepository : IGenericRepo<Store, Guid>
{
    Task<Store?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Store>> GetStoresDeliveringToAreaAsync(Guid areaId, CancellationToken cancellationToken = default);
}
