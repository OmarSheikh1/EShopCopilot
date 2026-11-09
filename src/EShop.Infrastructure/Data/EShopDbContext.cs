using EShop.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EShop.Infrastructure.Data;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Owns the SQLite model and applies audit/concurrency state during writes.
public sealed class EShopDbContext(DbContextOptions<EShopDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EShopDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndConcurrencyValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditAndConcurrencyValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditAndConcurrencyValues()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.UpdatedAt = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                }
            }

            if (entry.Entity is IConcurrencyTracked tracked &&
                entry.State == EntityState.Modified)
            {
                UpdateConcurrencyVersion(entry, tracked);
            }
        }
    }

    private static void UpdateConcurrencyVersion(EntityEntry entry, IConcurrencyTracked tracked)
    {
        var version = entry.Property(nameof(IConcurrencyTracked.Version));
        version.CurrentValue = tracked.Version + 1;
    }
}
