using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts;
using Nawly.Domain.Contracts.GenericReposPattern;
using Nawly.Domain.Contracts.SpecificationPattern;
using Nawly.Persistence.Evaluator;
using AppDbContext = Nawly.Persistence.Contexts.NawlyDbContext;

namespace Nawly.Persistence.Implementations.ReposPattern;

public class GenericRepo<TEntity, TKey>(AppDbContext dbContext)
    : IGenericRepo<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected AppDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllWithSpecAsync(
        ISpecifications<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator
            .GenerateQuery(DbSet, specifications)
            .ToListAsync(cancellationToken);

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(e => e.Id!.Equals(id), cancellationToken);

    public async Task<TEntity?> GetByIdWithSpecAsync(
        ISpecifications<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator
            .GenerateQuery(DbSet, specifications)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetCountAsync(
        ISpecifications<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator
            .GenerateCountQuery(DbSet, specifications)
            .CountAsync(cancellationToken);

    public async Task<bool> AnyAsync(
        ISpecifications<TEntity, TKey> specifications,
        CancellationToken cancellationToken = default)
        => await SpecificationEvaluator
            .GenerateCountQuery(DbSet, specifications)
            .AnyAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => await DbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity)
    {
        // Avoid forcing unchanged tracked navigations into Modified state, which bumps xmin concurrency tokens.
        if (DbContext.Entry(entity).State == EntityState.Detached)
            DbSet.Update(entity);
    }

    public void Delete(TEntity entity)
        => DbSet.Remove(entity);
}
