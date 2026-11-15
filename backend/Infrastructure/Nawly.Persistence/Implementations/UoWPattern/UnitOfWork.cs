using System.Collections.Concurrent;
using Nawly.Domain.Contracts;
using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Contracts.UnitOfWorkPattern;
using Nawly.Persistence.Implementations.ReposPattern;
using AppDbContext = Nawly.Persistence.NawlyDbContext.NawlyDbContext;

namespace Nawly.Persistence.Implementations.UoWPattern;

public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    public IGenericRepo<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
    {
        return (IGenericRepo<TEntity, TKey>)_repositories.GetOrAdd(
            typeof(TEntity),
            static (_, context) => new GenericRepo<TEntity, TKey>(context),
            dbContext);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => dbContext.DisposeAsync();
}
