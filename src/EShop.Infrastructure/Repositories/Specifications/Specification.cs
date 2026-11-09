using System.Linq.Expressions;
using EShop.Infrastructure.Filters;

namespace EShop.Infrastructure.Repositories.Specifications;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Supplies the shared implementation for reusable query specifications.
public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object?>>> _includes = [];
    private readonly List<SortExpression<T>> _orderings = [];

    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public IReadOnlyList<Expression<Func<T, object?>>> Includes => _includes;

    public IReadOnlyList<SortExpression<T>> Orderings => _orderings;

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    protected void AddInclude(Expression<Func<T, object?>> include) =>
        _includes.Add(include);

    protected void AddOrdering(
        Expression<Func<T, object?>> selector,
        bool ascending) =>
        _orderings.Add(new SortExpression<T>(selector, ascending));

    protected void ApplyPaging(int pageNumber, int pageSize)
    {
        QueryFilterHelper<T>.ValidatePaging(pageNumber, pageSize);
        Skip = checked((pageNumber - 1) * pageSize);
        Take = pageSize;
    }
}
