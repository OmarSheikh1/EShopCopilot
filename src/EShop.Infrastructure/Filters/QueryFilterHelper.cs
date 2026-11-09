using System.Linq.Expressions;
using System.Reflection;
using EShop.Infrastructure.Repositories.Specifications;

namespace EShop.Infrastructure.Filters;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Composes safe, strongly-typed filters, sorting, and bounded pagination.
public sealed class QueryFilterHelper<T> : Specification<T>
{
    private const int MaximumPageSize = 100;
    private static readonly MethodInfo ToLowerMethod =
        typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
    private static readonly MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private Expression<Func<T, bool>>? _criteria;

    public QueryFilterHelper<T> AddFilter(Expression<Func<T, bool>> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        _criteria = Combine(_criteria, filter);
        Criteria = _criteria;
        return this;
    }

    public QueryFilterHelper<T> AddTextSearch(
        TextSearchFilter filter,
        params Expression<Func<T, string?>>[] properties)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            return this;
        }

        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Length == 0)
        {
            throw new ArgumentException("At least one searchable property is required.", nameof(properties));
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var term = Expression.Constant(filter.SearchTerm.Trim().ToLower());
        Expression? match = null;
        foreach (var property in properties)
        {
            var body = new ReplaceParameterVisitor(property.Parameters[0], parameter)
                .Visit(property.Body)!;
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(
                Expression.Call(body, ToLowerMethod),
                ContainsMethod,
                term);
            var propertyMatch = Expression.AndAlso(notNull, contains);
            match = match is null ? propertyMatch : Expression.OrElse(match, propertyMatch);
        }

        var search = Expression.Lambda<Func<T, bool>>(match!, parameter);
        return AddFilter(search);
    }

    public QueryFilterHelper<T> AddDateRange(
        Expression<Func<T, DateTime>> property,
        DateRangeFilter range)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(range);
        if (range.From is not null && range.To is not null && range.From > range.To)
        {
            throw new ArgumentException("The start date must not be after the end date.", nameof(range));
        }

        if (range.From is { } from)
        {
            AddFilter(Expression.Lambda<Func<T, bool>>(
                Expression.GreaterThanOrEqual(
                    property.Body,
                    Expression.Constant(from)),
                property.Parameters));
        }

        if (range.To is { } to)
        {
            AddFilter(Expression.Lambda<Func<T, bool>>(
                Expression.LessThanOrEqual(
                    property.Body,
                    Expression.Constant(to)),
                property.Parameters));
        }

        return this;
    }

    public QueryFilterHelper<T> OrderBy(
        Expression<Func<T, object?>> property,
        bool ascending = true)
    {
        AddOrdering(property, ascending);
        return this;
    }

    public QueryFilterHelper<T> OrderBy(SortOptions<T> sortOptions)
    {
        ArgumentNullException.ThrowIfNull(sortOptions);
        AddOrdering(sortOptions.Selector, sortOptions.Ascending);
        return this;
    }

    public QueryFilterHelper<T> ThenBy(
        Expression<Func<T, object?>> property,
        bool ascending = true) =>
        OrderBy(property, ascending);

    public QueryFilterHelper<T> Paginate(PaginationOptions pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        ApplyPaging(pagination.PageNumber, pagination.PageSize);
        return this;
    }

    public ISpecification<T> BuildSpecification() => this;

    internal static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be positive.");
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 1 and {MaximumPageSize}.");
        }
    }

    private static Expression<Func<T, bool>> Combine(
        Expression<Func<T, bool>>? current,
        Expression<Func<T, bool>> next)
    {
        if (current is null)
        {
            return next;
        }

        var parameter = Expression.Parameter(typeof(T), "entity");
        var currentBody = new ReplaceParameterVisitor(current.Parameters[0], parameter).Visit(current.Body)!;
        var nextBody = new ReplaceParameterVisitor(next.Parameters[0], parameter).Visit(next.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(currentBody, nextBody), parameter);
    }

    private sealed class ReplaceParameterVisitor(
        ParameterExpression source,
        ParameterExpression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? replacement : base.VisitParameter(node);
    }
}
