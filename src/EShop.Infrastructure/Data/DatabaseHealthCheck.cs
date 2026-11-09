using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace EShop.Infrastructure.Data;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Reports database reachability through the Aspire readiness health check.
public sealed class DatabaseHealthCheck(
    IDbContextFactory<EShopDbContext> contextFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("SQLite database is reachable.")
            : HealthCheckResult.Unhealthy("SQLite database is not reachable.");
    }
}
