using Nawly.Domain.Contracts.GenericReposPattern;

namespace Nawly.Domain.Contracts.UnitOfWorkPattern;

public interface IUnitOfWork : IAsyncDisposable
{
    IGenericRepo<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class, IEntity<TKey>;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
