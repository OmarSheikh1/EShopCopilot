using System.Linq.Expressions;

namespace EShop.Infrastructure.Repositories.Specifications;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Carries typed criteria, includes, ordering, and paging into repositories.
public sealed record SortExpression<T>(
    Expression<Func<T, object?>> Selector,
    bool Ascending);

public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }

    IReadOnlyList<Expression<Func<T, object?>>> Includes { get; }

    IReadOnlyList<SortExpression<T>> Orderings { get; }

    int? Skip { get; }

    int? Take { get; }
}
