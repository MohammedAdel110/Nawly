using Microsoft.EntityFrameworkCore;
using Nawly.Domain.Contracts;
using Nawly.Domain.Contracts.SpecificationPattern;

namespace Nawly.Persistence.Evaluator;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GenerateQuery<TEntity, TKey>(
        IQueryable<TEntity> inputQuery,
        ISpecifications<TEntity, TKey> specifications)
        where TEntity : class, IEntity<TKey>
    {
        var query = ApplyFilteringAndBehavior(inputQuery, specifications, forCount: false);

        if (specifications.Includes.Count > 0)
        {
            query = specifications.Includes.Aggregate(
                query,
                static (current, include) => current.Include(include));
        }

        if (specifications.IncludeStrings.Count > 0)
        {
            query = specifications.IncludeStrings.Aggregate(
                query,
                static (current, include) => current.Include(include));
        }

        if (specifications.OrderByExpressions.Count > 0)
        {
            IOrderedQueryable<TEntity>? orderedQuery = null;
            foreach (var order in specifications.OrderByExpressions)
            {
                if (order.OrderByExpression is null)
                    continue;

                if (orderedQuery is null)
                {
                    orderedQuery = order.IsDescending
                        ? query.OrderByDescending(order.OrderByExpression)
                        : query.OrderBy(order.OrderByExpression);
                }
                else
                {
                    orderedQuery = order.IsDescending
                        ? orderedQuery.ThenByDescending(order.OrderByExpression)
                        : orderedQuery.ThenBy(order.OrderByExpression);
                }
            }

            if (orderedQuery is not null)
                query = orderedQuery;
        }

        if (specifications.IsPagingEnabled)
        {
            var skip = Math.Max(0, specifications.Skip);
            var take = Math.Max(0, specifications.Take);
            query = query.Skip(skip).Take(take);
        }

        return query;
    }

    public static IQueryable<TEntity> GenerateCountQuery<TEntity, TKey>(
        IQueryable<TEntity> inputQuery,
        ISpecifications<TEntity, TKey> specifications)
        where TEntity : class, IEntity<TKey>
        => ApplyFilteringAndBehavior(inputQuery, specifications, forCount: true);

    private static IQueryable<TEntity> ApplyFilteringAndBehavior<TEntity, TKey>(
        IQueryable<TEntity> inputQuery,
        ISpecifications<TEntity, TKey> specifications,
        bool forCount)
        where TEntity : class, IEntity<TKey>
    {
        var query = inputQuery;

        if (specifications.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        if (!forCount)
        {
            if (specifications.IsNoTracking)
                query = query.AsNoTracking();

            if (specifications.IsSplitQuery)
                query = query.AsSplitQuery();
        }
        else
        {
            query = query.AsNoTracking();
        }

        if (specifications.Criteria is not null)
            query = query.Where(specifications.Criteria);

        return query;
    }
}
