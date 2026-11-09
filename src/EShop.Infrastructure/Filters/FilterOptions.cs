using System.Linq.Expressions;

namespace EShop.Infrastructure.Filters;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Collects reusable filter settings for repository query composition.
public sealed record FilterOptions(
    TextSearchFilter? TextSearch = null,
    DateRangeFilter? DateRange = null,
    PaginationOptions? Pagination = null);

public sealed record PaginationOptions(int PageNumber = 1, int PageSize = 20);

public sealed record DateRangeFilter(DateTime? From, DateTime? To);

public sealed record TextSearchFilter(string SearchTerm);

public sealed record SortOptions<T>(
    Expression<Func<T, object?>> Selector,
    bool Ascending = true);
