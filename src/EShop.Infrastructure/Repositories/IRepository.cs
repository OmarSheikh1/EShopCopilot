using EShop.ApiService.Models;
using EShop.Infrastructure.Repositories.Specifications;

namespace EShop.Infrastructure.Repositories;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Defines async entity operations without leaking EF Core queryables.
public interface IRepository<T> where T : class, IAuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> FindAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Delete(T entity);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
