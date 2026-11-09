# Database Infrastructure: EF Core + SQLite with Clean Architecture Patterns

**Feature Name:** Database Infrastructure  
**Change ID:** CHG-20261106-003-DATABASE-INFRASTRUCTURE  
**Status:** Planning  
**Date:** 2026-11-06

---

## Executive Summary

This plan introduces persistent data storage to EShopCopilot by replacing in-memory storage with a production-grade EF Core + SQLite infrastructure. The implementation follows **Clean Architecture**, applies **SOLID principles** (especially Dependency Inversion), and uses **Repository Pattern**, **Unit of Work**, and **Specification Pattern** to maintain clean abstractions and testability.

The design ensures:
- **No business logic leakage** into persistence concerns
- **No IQueryable exposure** outside Infrastructure
- **Strongly-typed query expressions** for type safety
- **Reusable generic and entity-specific repositories**
- **Advanced filtering, search, sorting, pagination, and date-range queries**
- **Seamless Aspire integration** for service discovery
- **Existing functionality preserved** with backward-compatible data migration

---

## Architecture Overview

### Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  EShop.Web (Blazor Frontend)                                │
│  - Pages, Components, API Clients                           │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP/REST
┌────────────────────▼────────────────────────────────────────┐
│  EShop.ApiService (Presentation)                            │
│  - Minimal API Endpoints                                    │
│  - DTOs & Contracts                                         │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│  EShop.Application (Business Logic)                         │
│  - Services (ProductService, OrderService)                  │
│  - Repository Interfaces                                   │
│  - Specifications                                           │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│  EShop.Infrastructure (Data Access)                         │
│  - DbContext (EShopDbContext)                              │
│  - Generic Repository<T>                                   │
│  - Specific Repositories (ProductRepository, OrderRepo...)  │
│  - Unit of Work                                            │
│  - Query Filters & Specifications                          │
│  - Migrations                                              │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│  SQLite Database                                            │
│  - Products Table                                           │
│  - Orders Table                                            │
│  - OrderItems Table                                         │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Principles

1. **Dependency Inversion**: Services depend on abstractions (interfaces), not concrete implementations
2. **Repository Pattern**: Encapsulates data access logic; provides collection-like interface
3. **Unit of Work Pattern**: Manages transaction scope and repository coordination
4. **Specification Pattern**: Encapsulates query logic in type-safe, reusable objects
5. **Query Filter Helper**: Centralizes filtering, search, sorting, pagination, and date-range logic
6. **No IQueryable Leakage**: Infrastructure layers expose only typed results, never IQueryable

---

## File Structure & Deliverables

### New Projects

#### `EShop.Infrastructure.csproj`
```
EShop.Infrastructure/
├── EShop.Infrastructure.csproj
├── Data/
│   ├── EShopDbContext.cs
│   ├── DbContextOptionsBuilderExtensions.cs
│   └── Migrations/
│       ├── 20261106000001_InitialCreate.cs
│       ├── 20261106000001_InitialCreate.Designer.cs
│       └── EShopDbContextModelSnapshot.cs
├── Repositories/
│   ├── IRepository.cs
│   ├── IUnitOfWork.cs
│   ├── Repository.cs
│   ├── UnitOfWork.cs
│   ├── Product/
│   │   ├── IProductRepository.cs
│   │   └── ProductRepository.cs
│   ├── Order/
│   │   ├── IOrderRepository.cs
│   │   └── OrderRepository.cs
│   └── OrderItem/
│       ├── IOrderItemRepository.cs
│       └── OrderItemRepository.cs
├── Specifications/
│   ├── ISpecification.cs
│   ├── Specification.cs
│   ├── Product/
│   │   ├── ProductByIdSpec.cs
│   │   ├── ProductByCategorySpec.cs
│   │   ├── ActiveProductsSpec.cs
│   │   └── ProductSearchSpec.cs
│   └── Order/
│       ├── OrderByIdSpec.cs
│       ├── OrderByCustomerEmailSpec.cs
│       └── OrdersByDateRangeSpec.cs
├── Filters/
│   ├── QueryFilterHelper.cs
│   ├── FilterOptions.cs
│   ├── PaginationOptions.cs
│   ├── SortOptions.cs
│   ├── DateRangeFilter.cs
│   ├── TextSearchFilter.cs
│   └── StatusCategoryFilter.cs
├── Configurations/
│   ├── ProductEntityConfiguration.cs
│   ├── OrderEntityConfiguration.cs
│   └── OrderItemEntityConfiguration.cs
└── ServiceCollectionExtensions.cs
```

### Modified Projects

#### `EShop.ApiService.csproj`
- Add project reference to `EShop.Infrastructure`
- Add NuGet packages: `Microsoft.EntityFrameworkCore.Sqlite`

#### `EShop.AppHost`
- Wire EShop.Infrastructure into Aspire service configuration
- Add health checks for database connectivity

#### `src/EShop.ApiService/Program.cs`
- Register `EShop.Infrastructure` services via DI
- Add database initialization/migration logic
- Configure EShopDbContext

---

## Detailed Component Descriptions

### 1. DbContext & Database Configuration

#### `EShopDbContext.cs`
```csharp
public class EShopDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public EShopDbContext(DbContextOptions<EShopDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EShopDbContext).Assembly);
        
        // Seed initial data
        modelBuilder.Entity<Product>().HasData(GetSeedProducts());
        modelBuilder.Entity<Order>().HasData(GetSeedOrders());
        modelBuilder.Entity<OrderItem>().HasData(GetSeedOrderItems());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit: Set UpdatedAt on modified entities
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    private List<Product> GetSeedProducts() { /* seed data from existing ProductService */ }
    private List<Order> GetSeedOrders() { /* seed data from existing OrderService */ }
    private List<OrderItem> GetSeedOrderItems() { /* construct from seed orders */ }
}
```

#### Entity Configurations
- **ProductEntityConfiguration**: Fluent API configuration for Products table
  - Primary key, constraints, indexes, default values
  - Shadow property for concurrency (RowVersion)
  
- **OrderEntityConfiguration**: Fluent API configuration for Orders table
  - One-to-many relationship with OrderItems
  - Cascade delete behavior
  - Indexes on CustomerEmail for efficient lookup
  
- **OrderItemEntityConfiguration**: Fluent API configuration for OrderItems table
  - Foreign key to Product and Order
  - Precision for decimal prices

### 2. Repository Pattern

#### `IRepository<T>` Interface
```csharp
public interface IRepository<T> where T : class, IAuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(
        ISpecification<T> spec, 
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        ISpecification<T> spec, 
        CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
```

#### Generic `Repository<T>` Implementation
- Uses DbSet from EShopDbContext
- Applies specifications to IQueryable via ApplySpecification
- Translates specifications to SQL via EF Core's query provider
- Never exposes IQueryable; always returns IReadOnlyList<T>
- Supports async operations with CancellationToken propagation

#### Entity-Specific Repositories

**IProductRepository & ProductRepository**
- Inherits from Repository<Product>
- Adds product-specific queries (by category, active status, search)
- Uses built-in specifications (ProductByCategorySpec, ActiveProductsSpec, etc.)

**IOrderRepository & OrderRepository**
- Inherits from Repository<Order>
- Adds order-specific queries (by customer email, date range, status)
- Uses order-specific specifications

**IOrderItemRepository & OrderItemRepository**
- Inherits from Repository<OrderItem>
- Queries typically filtered through orders

### 3. Unit of Work Pattern

#### `IUnitOfWork` Interface
```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IAsyncTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

#### `UnitOfWork` Implementation
- Manages repository instances
- Coordinates SaveChangesAsync across all repositories
- Handles database transactions via BeginTransactionAsync
- Implements IAsyncDisposable for proper resource cleanup

### 4. Specification Pattern

#### `ISpecification<T>` Interface
```csharp
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool IsPagingEnabled { get; }
}
```

#### Base `Specification<T>` Class
- Holds criteria (WHERE clause)
- Includes (JOIN/INCLUDE)
- Order by (single sort column)
- Pagination (Take/Skip)
- Applied by Repository via ApplySpecification extension

#### Concrete Specifications

**Product Specifications:**
- `ProductByIdSpec(Guid id)` — Single product by ID
- `ActiveProductsSpec()` — All active products
- `ProductByCategorySpec(string category)` — Filter by category
- `ProductSearchSpec(string searchTerm)` — Text search on Name/Description
- `ProductSearchWithPaginationSpec(string searchTerm, int pageNumber, int pageSize)` — Paginated search

**Order Specifications:**
- `OrderByIdSpec(Guid id)` — Single order by ID
- `OrderByCustomerEmailSpec(string email)` — Orders for a customer
- `OrdersByDateRangeSpec(DateTime from, DateTime to)` — Date range filtering
- `OrdersByStatusSpec(OrderStatus status)` — Filter by status
- `RecentOrdersSpec(int days)` — Orders from last N days

### 5. Query Filter Helper

#### `QueryFilterHelper` Class
Advanced filtering utility with support for:

**Core Features:**
1. **Filtering**: Property-based equality filters (e.g., Category = "Accessories")
2. **Text Search**: Full-text search across Name, Description, etc.
3. **Sorting**: Multi-column sort by name/price/date with ascending/descending direction
4. **Pagination**: Offset/limit or page-number/page-size patterns
5. **Date Range**: Filter by CreatedAt/UpdatedAt between dates
6. **Status/Category**: Enum and string filters for categorical data

**Strongly-Typed Expression Building:**
- Uses Expression<Func<T, bool>> for type-safe criteria
- Translates to SQL via EF Core's query provider
- No string-based dynamic LINQ

**Usage Example:**
```csharp
var filter = new QueryFilterHelper<Product>()
    .AddTextSearch("mouse", p => p.Name, p => p.Description)
    .AddFilter(p => p.Category == "Accessories")
    .AddFilter(p => p.IsActive)
    .OrderBy(p => p.Price, ascending: true)
    .Paginate(pageNumber: 1, pageSize: 10);

var products = await _productRepository.FindAsync(
    filter.BuildSpecification(), 
    cancellationToken);
```

**Key Classes:**
- `QueryFilterHelper<T>`: Fluent API for building filters
- `FilterOptions`: Configuration for filters
- `PaginationOptions`: Page number, page size, total count
- `SortOptions`: Sort column, ascending/descending
- `DateRangeFilter`: From/to dates for filtering
- `TextSearchFilter`: Search term and searchable properties
- `StatusCategoryFilter`: Enum/category filters

---

## Data Models

### Product Entity
```csharp
public class Product : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[]? RowVersion { get; set; } // Optimistic concurrency
}
```

### Order Entity
```csharp
public class Order : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
    [Timestamp] public byte[]? RowVersion { get; set; } // Optimistic concurrency
}
```

### OrderItem Entity
```csharp
public class OrderItem : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; } // Foreign key
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Order Order { get; set; } = null!;
}
```

### Interfaces
- `IAuditableEntity`: Provides CreatedAt, UpdatedAt properties for audit trails

---

## Implementation Steps

### Phase 1: Infrastructure Project Setup
1. Create new `EShop.Infrastructure` project (.csproj)
2. Add project references and NuGet dependencies
3. Create folder structure for Data, Repositories, Specifications, Filters
4. Create base interfaces and abstract classes

### Phase 2: DbContext & Migrations
5. Implement `EShopDbContext` with DbSets
6. Implement entity configurations (Fluent API)
7. Create initial migration: `20261106000001_InitialCreate`
8. Seed existing product and order data

### Phase 3: Repository Pattern
9. Implement `IRepository<T>` and generic `Repository<T>`
10. Implement specific repository interfaces and implementations
11. Add ApplySpecification extension method

### Phase 4: Unit of Work
12. Implement `IUnitOfWork` and `UnitOfWork` class
13. Wire repositories into Unit of Work
14. Add transaction support

### Phase 5: Specifications & Query Filters
15. Implement base `ISpecification<T>` and `Specification<T>`
16. Create product and order specifications
17. Implement `QueryFilterHelper` and filter classes
18. Add fluent API methods for common query patterns

### Phase 6: Dependency Injection
19. Create `ServiceCollectionExtensions` for Infrastructure DI
20. Register DbContext, repositories, unit of work
21. Add to `EShop.ApiService/Program.cs`

### Phase 7: Service Migration
22. Refactor `ProductService` to use repositories
23. Refactor `OrderService` to use repositories
24. Update endpoints to call services (no direct repository access)
25. Remove in-memory storage

### Phase 8: Aspire Integration
26. Configure Aspire AppHost for SQLite database
27. Add health checks for database
28. Add OpenTelemetry traces for EF Core queries

### Phase 9: Testing & Validation
29. Create repository unit tests
30. Create specification unit tests
31. Create query filter unit tests
32. Create integration tests with real DbContext
33. Run full test suite
34. Smoke test with Aspire AppHost

### Phase 10: Documentation & Cleanup
35. Create MIGRATION.md with data migration steps
36. Update README.md with Infrastructure architecture
37. Update CHANGELOG-AI.md with change details
38. Clean up temporary files

---

## Data Migration Strategy

### Existing Data Preservation
1. **Products**: Map in-memory seeded products to database via OnModelCreating seed data
2. **Orders**: Map in-memory seeded orders to database via OnModelCreating seed data
3. **OrderItems**: Reconstructed from order items in seed data

### Migration Process
1. On application startup, EF Core checks if database exists
2. If not, creates database and runs all migrations
3. Seed data is applied during initial migration
4. Existing endpoints continue to work with new persistence layer

### Rollback Plan
- Database file: `data.db` (SQLite file in app directory or configured path)
- Delete file to reset to fresh state
- Application auto-recreates on next startup

---

## Dependency Injection & Service Registration

### Infrastructure ServiceCollection Extension
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEShopInfrastructure(
        this IServiceCollection services, 
        string connectionString)
    {
        services.AddDbContext<EShopDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();

        return services;
    }
}
```

### Program.cs Registration
```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=data.db;Cache=Shared";

builder.Services.AddEShopInfrastructure(connectionString);

// Enable EF Core query logging in Development
if (app.Environment.IsDevelopment())
{
    services.AddLogging(config => config.AddConsole());
}
```

---

## Aspire Integration

### AppHost Configuration
```csharp
var apiService = builder.AddProject<Projects.EShop_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", 
        "Data Source=/data/eshop.db;Cache=Shared");

// Volume for SQLite database persistence
builder.AddVolume("eshop-data", "/data");
```

### Health Checks
- Add health check endpoint: `GET /health`
- Include database connectivity check
- Report status: Healthy, Degraded, Unhealthy

---

## Concurrency & Transaction Management

### Optimistic Concurrency
- Use `[Timestamp]` attribute (RowVersion byte[]) on entities
- EF Core automatically detects conflicts on SaveChangesAsync
- Catch `DbUpdateConcurrencyException` in services

### Pessimistic Locking
- Use `IAsyncTransaction` from Unit of Work for long-running operations
- Example: Checkout transaction should lock order and verify inventory

### Transaction Isolation
- Default: Read Committed (SQLite default)
- Order creation: Serializable for checkout operations
- Product updates: Serializable to prevent race conditions

---

## API Contract Changes

### Backward Compatibility
- All existing endpoints remain unchanged
- Service responses (DTOs) remain identical
- Persistence layer is transparent to API consumers

### New Internal Capabilities
- Services now support advanced filtering (via QueryFilterHelper)
- Services can use specifications for complex queries
- Future endpoints can leverage new filtering infrastructure

---

## Testing Strategy

### Unit Tests

#### Repository Tests
- Test CRUD operations on generic repository
- Test specification application
- Test filtering, sorting, pagination
- Mock DbContext using Moq or in-memory database

#### Specification Tests
- Verify criteria expressions
- Verify includes and relationships
- Test pagination boundaries

#### Query Filter Tests
- Test filter combinations
- Test SQL translation correctness
- Verify pagination and sorting

#### Service Tests
- Mock repositories
- Test business logic isolation from persistence
- Test error handling and edge cases

### Integration Tests

#### DbContext Tests
- Use SQLite in-memory database
- Test full CRUD lifecycle
- Test migrations
- Test data seeding
- Test audit properties (CreatedAt, UpdatedAt)

#### End-to-End Tests
- Use WebApplicationFactory<Program>
- Test endpoints with real database
- Verify request/response contracts
- Test error scenarios

### Test Database Setup
```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private DbContextOptions<EShopDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<EShopDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
    }

    public async Task InitializeAsync()
    {
        using var context = new EShopDbContext(CreateOptions());
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

---

## Acceptance Criteria

### Infrastructure Layer
- [ ] `EShop.Infrastructure` project created with all required files
- [ ] `EShopDbContext` properly configured with DbSets for Product, Order, OrderItem
- [ ] Entity configurations applied via Fluent API
- [ ] Initial migration created and runs successfully
- [ ] Generic `Repository<T>` supports CRUD and specification queries
- [ ] Product, Order, and OrderItem repositories implemented
- [ ] `IUnitOfWork` coordinates repositories and transactions
- [ ] All repositories support async operations with CancellationToken
- [ ] No IQueryable exposed outside Infrastructure layer

### Specification Pattern
- [ ] `ISpecification<T>` and base implementation complete
- [ ] Product specifications (by ID, category, active, search)
- [ ] Order specifications (by ID, email, date range, status)
- [ ] Specifications are strongly-typed (Expression-based)
- [ ] Specifications translate correctly to SQL

### Query Filter Helper
- [ ] `QueryFilterHelper<T>` supports text search
- [ ] Filtering by property values
- [ ] Multi-column sorting (ascending/descending)
- [ ] Pagination (page number and page size)
- [ ] Date range filtering
- [ ] Status/category filters
- [ ] Fluent API for filter composition
- [ ] Generated SQL is efficient and correctly parameterized

### Service Migration
- [ ] `ProductService` refactored to use `IProductRepository`
- [ ] `OrderService` refactored to use `IOrderRepository`
- [ ] All existing service methods work with database
- [ ] No business logic in endpoints
- [ ] No in-memory storage remaining

### Data Preservation
- [ ] All existing products seeded in database
- [ ] All existing orders seeded in database
- [ ] Existing customer order data preserved
- [ ] Order IDs and product IDs remain unchanged (GUIDs)

### Aspire Integration
- [ ] AppHost configures SQLite connection string
- [ ] Database health check passes
- [ ] Service discovery works for database
- [ ] Connection pooling configured
- [ ] OpenTelemetry traces EF Core queries

### Testing
- [ ] Unit tests for repositories (>=80% coverage)
- [ ] Unit tests for specifications (>=80% coverage)
- [ ] Unit tests for query filters (>=80% coverage)
- [ ] Integration tests for DbContext
- [ ] Integration tests for full endpoint-to-database flows
- [ ] All tests pass: `dotnet test src\EShop.sln`

### Build & Code Quality
- [ ] Solution builds cleanly: `dotnet build src\EShop.sln`
- [ ] No compiler warnings
- [ ] No NuGet vulnerabilities: `dotnet list package --vulnerable`
- [ ] Follows repository architecture standards (aspire.instructions.md)
- [ ] Change tracking comments added (change-tracking.instructions.md)
- [ ] CHANGELOG-AI.md updated with change details

### Verification
- [ ] Aspire AppHost starts and reports all services healthy
- [ ] Products endpoint returns data from database
- [ ] Orders endpoint returns data from database
- [ ] Checkout creates orders in database
- [ ] My Orders retrieves orders from database
- [ ] Filtering/search works on products
- [ ] Pagination works on collections

---

## Dependencies & External References

### NuGet Packages to Add
- `Microsoft.EntityFrameworkCore` (v10.0.12 or current)
- `Microsoft.EntityFrameworkCore.Sqlite` (v10.0.12 or current)
- `Microsoft.EntityFrameworkCore.Design` (for migrations)

### Internal Project References
- `EShop.ApiService` → `EShop.Infrastructure`
- Tests → `EShop.Infrastructure`

### Database
- **SQLite 3.x** (file-based, no separate server)
- Default path: `data.db` or via connection string configuration

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Data loss during migration | Low | High | Seed data from existing services; test migrations locally first |
| Performance regression | Medium | High | Add indexes on frequently-queried columns (CustomerEmail, Category); profile queries with EF query logs |
| Concurrency issues | Medium | Medium | Use optimistic concurrency (RowVersion); add tests for race conditions |
| Breaking existing endpoints | Low | High | Comprehensive integration tests; verify response DTOs unchanged |
| Circular dependencies | Low | High | Clear layering: only Application depends on Infrastructure; services never return entities |
| Query N+1 problems | Medium | Medium | Use specifications with includes; log N+1 detection tools in development |

---

## Estimated Effort

| Phase | Complexity | Effort | Notes |
|-------|-----------|--------|-------|
| Infrastructure Setup | Low | 1-2 hrs | Project creation, folder structure |
| DbContext & Migrations | Medium | 2-3 hrs | Fluent API config, seeding |
| Repository Pattern | Medium | 3-4 hrs | Generic + specific repos |
| Unit of Work | Low | 1-2 hrs | Coordination layer |
| Specifications | Medium | 2-3 hrs | Type-safe query encapsulation |
| Query Filter Helper | Medium | 2-3 hrs | Advanced filtering features |
| DI & Program.cs | Low | 1 hr | Service registration |
| Service Migration | Medium | 2-3 hrs | Refactor existing services |
| Aspire Integration | Low | 1 hr | Configuration |
| Testing | High | 4-6 hrs | Unit + integration tests |
| **Total** | — | **20-27 hrs** | **3-4 days at 8 hrs/day** |

---

## Success Metrics

1. **Functionality**: All existing endpoints work identically with persistent storage
2. **Performance**: Query response times < 100ms for typical operations (products, orders)
3. **Testability**: >=80% code coverage on Infrastructure layer
4. **Maintainability**: All business logic remains in services; persistence fully isolated
5. **Scalability**: Query patterns are efficient; N+1 queries eliminated via specifications
6. **Documentation**: Architecture diagrams, code comments, and MIGRATION.md provided

---

## Next Steps (Post-Implementation)

### Short Term
1. Implement feature endpoints using new Infrastructure (e.g., advanced product search)
2. Add database query performance monitoring
3. Configure automated backups for SQLite

### Medium Term
1. Migrate from SQLite to PostgreSQL for production
2. Implement soft deletes for audit trail
3. Add audit logging (who changed what, when)
4. Implement change data capture (CDC) for real-time sync

### Long Term
1. Event sourcing for order state transitions
2. Read/write separation (CQRS pattern)
3. Full-text search via Elasticsearch
4. Distributed caching with Redis

---

## Change Tracking

**Change ID**: CHG-20261106-003-DATABASE-INFRASTRUCTURE  
**Files to Create**: 30+ (Infrastructure project)  
**Files to Modify**: 5-7 (ApiService, AppHost, existing services)  
**Estimated Lines Added**: 2,000+  
**Estimated Lines Modified**: 500+  

---

## Related Documentation

- [Repository Instructions](../.github/instructions/domain.instructions.md)
- [Architecture Standards](../.github/instructions/architecture.instructions.md)
- [Aspire Standards](../.github/instructions/aspire.instructions.md)
- [Change Tracking](../.github/instructions/change-tracking.instructions.md)
- [Existing Plans](./shopping-cart.md), [My Orders](./my-orders.md)

---

**Plan Created**: 2026-11-06  
**Status**: Ready for Review & Approval  
**Next Action**: Await approval before implementation
