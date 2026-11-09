using EShop.Infrastructure.Data;
using EShop.Infrastructure.Repositories;
using EShop.Infrastructure.Repositories.Order;
using EShop.Infrastructure.Repositories.OrderItem;
using EShop.Infrastructure.Repositories.Product;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Infrastructure;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Registers SQLite persistence, repositories, unit of work, and database health.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEShopInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
        if (sqliteConnection.DataSource != ":memory:" &&
            !Path.IsPathRooted(sqliteConnection.DataSource))
        {
            sqliteConnection.DataSource = Path.GetFullPath(
                sqliteConnection.DataSource,
                contentRootPath);
        }

        var databaseDirectory = Path.GetDirectoryName(sqliteConnection.DataSource);
        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        services.AddDbContextFactory<EShopDbContext>(options =>
            options.UseSqlite(sqliteConnection.ConnectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        return services;
    }

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EShopDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
