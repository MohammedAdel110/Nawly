using Nawly.Domain.Contracts.SpecificationPattern;

namespace Nawly.Domain.Contracts.GenericReposPattern;

public interface IGenericRepo<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllWithSpecAsync(ISpecifications<TEntity, TKey> specifications, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdWithSpecAsync(ISpecifications<TEntity, TKey> specifications, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(ISpecifications<TEntity, TKey> specifications, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecifications<TEntity, TKey> specifications, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
