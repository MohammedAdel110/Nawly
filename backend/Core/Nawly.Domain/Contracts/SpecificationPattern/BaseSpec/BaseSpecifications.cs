using System.Linq.Expressions;

namespace Nawly.Domain.Contracts.SpecificationPattern.BaseSpec;

public abstract class BaseSpecifications<TEntity, TKey> : ISpecifications<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected BaseSpecifications()
    {
        Criteria = _ => true;
    }

    protected BaseSpecifications(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = criteria;
    }

    public Expression<Func<TEntity, bool>> Criteria { get; protected set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public List<OrderExpressionInfo<TEntity>> OrderByExpressions { get; } = [];
    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool IsNoTracking { get; private set; }
    public bool IsSplitQuery { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
        => Includes.Add(includeExpression);

    protected void AddInclude(string includeString)
        => IncludeStrings.Add(includeString);

    protected void AddOrderBy(Expression<Func<TEntity, object>> orderByExpression, bool isDescending = false)
        => OrderByExpressions.Add(new OrderExpressionInfo<TEntity>
        {
            OrderByExpression = orderByExpression,
            IsDescending = isDescending
        });

    protected void ApplyPaging(int pageSize, int pageIndex)
    {
        var safeIndex = pageIndex < 1 ? 1 : pageIndex;
        var safeSize = pageSize < 1 ? 1 : pageSize;
        Skip = (safeIndex - 1) * safeSize;
        Take = safeSize;
        IsPagingEnabled = true;
    }

    protected void ApplyAsNoTracking() => IsNoTracking = true;
    protected void ApplySplitQuery() => IsSplitQuery = true;
    protected void ApplyIgnoreQueryFilters() => IgnoreQueryFilters = true;
}
