using System.Linq.Expressions;

namespace Nawly.Domain.Contracts.SpecificationPattern;

public sealed class OrderExpressionInfo<TEntity>
{
    public Expression<Func<TEntity, object>>? OrderByExpression { get; set; }
    public bool IsDescending { get; set; }
}
