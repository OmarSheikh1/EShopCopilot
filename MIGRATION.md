# SQLite database operations

The API uses Entity Framework Core with SQLite. The default connection string is
`Data Source=eshop.db;Cache=Shared`; relative paths are resolved from the API
content root. The AppHost forwards its `ConnectionStrings:DefaultConnection`
setting to the API, so configure that setting in AppHost user secrets or the
environment to select another database file.

## Startup and seed data

The API applies pending migrations before accepting requests. The initial
migration creates the product, order, and order-item tables and seeds the five
existing products and five sample orders with their original IDs. New writes
are persisted; restarting the API does not reseed or reset the database.
`/health` includes a database connectivity check in Development.

Products and orders use integer concurrency tokens because SQLite does not
provide an auto-generated `rowversion` column. A stale write fails with a
`409 Conflict`. Order items reference their parent order with cascade deletion;
their `ProductId` is retained as a historical catalog reference so deleting a
product does not invalidate an existing order snapshot.

## Creating a migration

Install the EF Core CLI matching the EF Core package version, then run from the
repository root:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef migrations add <MigrationName> `
  --project src\EShop.Infrastructure `
  --startup-project src\EShop.Infrastructure `
  --output-dir Data\Migrations
```

The Infrastructure project includes a design-time context factory so migration
commands do not start the API or update a live database. Review generated
migrations before applying them.

## Backup and reset

Stop the API before copying the SQLite database file. Keep a verified backup
before deploying schema changes. To use a non-default database, configure
`ConnectionStrings__DefaultConnection`, for example:

```text
Data Source=C:\data\eshop.db;Cache=Shared
```

Deleting the database file is destructive and resets data to the initial seed
when the API next starts.
