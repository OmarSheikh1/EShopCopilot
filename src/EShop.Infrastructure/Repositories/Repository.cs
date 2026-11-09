using System.Linq.Expressions;
using EShop.ApiService.Models;
using EShop.Infrastructure.Data;
using EShop.Infrastructure.Repositories.Specifications;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Repositories;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Implements specification-based EF Core CRUD without exposing IQueryable.
public class Repository<T>(EShopDbContext context) : IRepository<T>
    where T : class, IAuditableEntity
{
    private protected EShopDbContext Context { get; } = context;

    private protected DbSet<T> Set => Context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default) =>
        await ApplySpecification(specification).ToListAsync(cancellationToken);

    public virtual async Task<int> CountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsQueryable();
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public virtual void Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }

    public virtual async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        Delete(entity);
        return true;
    }

    private protected IQueryable<T> ApplySpecification(ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        IQueryable<T> query = Set;
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        IOrderedQueryable<T>? orderedQuery = null;
        foreach (var ordering in specification.Orderings)
        {
            if (orderedQuery is null)
            {
                orderedQuery = ordering.Ascending
                    ? query.OrderBy(ordering.Selector)
                    : query.OrderByDescending(ordering.Selector);
            }
            else
            {
                orderedQuery = ordering.Ascending
                    ? orderedQuery.ThenBy(ordering.Selector)
                    : orderedQuery.ThenByDescending(ordering.Selector);
            }
        }

        if (orderedQuery is not null)
        {
            query = orderedQuery;
        }

        if (specification.Skip is { } skip)
        {
            query = query.Skip(skip);
        }

        if (specification.Take is { } take)
        {
            query = query.Take(take);
        }

        return query;
    }
}
