using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EShop.ApiService.Tests;

// CHG-20261106-003-DATABASE-INFRASTRUCTURE
// Purpose: Gives each HTTP integration test an isolated SQLite database.
internal sealed class DatabaseTestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;
    private readonly bool _deleteDatabaseOnDispose;

    public DatabaseTestApplicationFactory(string? databasePath = null)
    {
        _deleteDatabaseOnDispose = databasePath is null;
        _databasePath = databasePath ?? Path.Combine(
            Path.GetTempPath(),
            $"eshop-tests-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = $"Data Source={_databasePath};Pooling=False";
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            }));
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!_deleteDatabaseOnDispose)
        {
            return;
        }

        DeleteDatabaseFiles();
    }

    internal static void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var filePath in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    private void DeleteDatabaseFiles() => DeleteDatabaseFiles(_databasePath);
}
