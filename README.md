# EShop – .NET Aspire Distributed Application

A cloud-native eShop built with **.NET 10** and **Aspire 13**, featuring a Minimal API backend and a Blazor Server frontend. Supports full CRUD for **Products**, **Shopping Cart** and **Orders**.

## Database

The API persists products, orders, and order items in SQLite through EF Core. On startup it applies pending migrations and seeds the existing catalog and sample orders. By default the database is `src/EShop.ApiService/eshop.db`; override `ConnectionStrings:DefaultConnection` (or `ConnectionStrings__DefaultConnection` in Aspire) to choose another file.

The persistence implementation lives in [src/EShop.Infrastructure](src/EShop.Infrastructure). See [MIGRATION.md](MIGRATION.md) for migration and backup instructions.
