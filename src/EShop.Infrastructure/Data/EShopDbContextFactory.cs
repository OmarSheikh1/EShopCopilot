using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EShop.Infrastructure.Data;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Enables dotnet-ef tooling without starting the API or mutating its database.
public sealed class EShopDbContextFactory : IDesignTimeDbContextFactory<EShopDbContext>
{
    public EShopDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection") ?? "Data Source=eshop.db;Cache=Shared";
        var options = new DbContextOptionsBuilder<EShopDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new EShopDbContext(options);
    }
}
