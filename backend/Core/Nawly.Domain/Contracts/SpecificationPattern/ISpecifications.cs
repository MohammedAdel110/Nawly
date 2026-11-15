using System.Linq.Expressions;

namespace Nawly.Domain.Contracts.SpecificationPattern;

public interface ISpecifications<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Expression<Func<TEntity, bool>> Criteria { get; }
    List<Expression<Func<TEntity, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    List<OrderExpressionInfo<TEntity>> OrderByExpressions { get; }
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
    bool IsNoTracking { get; }
    bool IsSplitQuery { get; }
    bool IgnoreQueryFilters { get; }
}
