# Database Infrastructure: Before & After Comparison

## Current State (In-Memory Storage)

```
┌─────────────────────────────────────────┐
│ EShop.ApiService                        │
├─────────────────────────────────────────┤
│                                         │
│ ProductService (in-memory)              │
│ └─ Dictionary<Guid, Product>            │
│    └─ Thread-safe lock(_sync)           │
│                                         │
│ OrderService (in-memory)                │
│ └─ Dictionary<Guid, Order>              │
│    └─ Thread-safe lock(_sync)           │
│                                         │
│ Endpoints call services directly        │
│ No abstraction layer                    │
│ No query patterns                       │
│                                         │
└─────────────────────────────────────────┘
```

### Limitations

| Limitation | Impact |
|------------|--------|
| **Data Loss on Restart** | All orders/products lost when app stops |
| **No Scalability** | Can't run multiple app instances (in-memory state not shared) |
| **Limited Querying** | Manual LINQ-to-Objects filtering on collections in memory |
| **No Transaction Support** | No ACID guarantees across operations |
| **Tight Coupling** | Services directly depend on concrete data structures |
| **No Audit Trail** | Who changed what, when? Not tracked |
| **Testing Difficulty** | Hard to mock service storage behavior |
| **Concurrency Issues** | Only thread-safe within single app instance |

---

## New State (EF Core + SQLite with Clean Architecture)

```
┌──────────────────────────────────────────────────────────────────┐
│ EShop.ApiService (Presentation)                                  │
│ ├─ Endpoints (thin HTTP handlers)                                │
│ └─ DTOs/Contracts (API contracts only)                           │
└────────────────┬─────────────────────────────────────────────────┘
                 │ Depends on (DI)
┌────────────────▼─────────────────────────────────────────────────┐
│ EShop.Application (Services)                                     │
│ ├─ ProductService                                                │
│ │  └─ Depends on IProductRepository (interface)                  │
│ ├─ OrderService                                                  │
│ │  └─ Depends on IOrderRepository (interface)                    │
│ └─ Repository Interfaces (no implementation here)                │
└────────────────┬─────────────────────────────────────────────────┘
                 │ Depends on (DI)
┌────────────────▼─────────────────────────────────────────────────┐
│ EShop.Infrastructure (Data Access)                               │
│ ├─ EShopDbContext (Entity Framework)                             │
│ │  └─ DbSet<Product>, DbSet<Order>, DbSet<OrderItem>            │
│ ├─ Generic Repository<T> Implementation                          │
│ │  └─ CRUD operations + specification support                   │
│ ├─ Specific Repositories (ProductRepository, OrderRepository)    │
│ │  └─ Entity-specific queries                                   │
│ ├─ Unit of Work (transaction coordination)                       │
│ ├─ Specifications (type-safe query encapsulation)                │
│ └─ Query Filter Helper (advanced filtering/search/sort/paginate) │
└────────────────┬─────────────────────────────────────────────────┘
                 │ Uses EF Core Provider
┌────────────────▼─────────────────────────────────────────────────┐
│ SQLite Database                                                  │
│ ├─ Products Table (persisted)                                   │
│ ├─ Orders Table (persisted)                                     │
│ └─ OrderItems Table (persisted)                                 │
└──────────────────────────────────────────────────────────────────┘
```

### Advantages

| Advantage | Benefit |
|-----------|---------|
| **Persistent Storage** | Data survives app restarts |
| **Scalability** | Multiple app instances can share same database |
| **Advanced Querying** | Specifications + Query Filter Helper for complex queries |
| **ACID Transactions** | Unit of Work ensures data consistency |
| **Loose Coupling** | Services depend on abstractions (interfaces) |
| **Audit Trail** | CreatedAt/UpdatedAt timestamps on all entities |
| **Testable** | Mock repositories in unit tests; in-memory DB for integration tests |
| **Distributed** | Concurrent requests from multiple app instances supported |
| **Query Performance** | SQL indexes, query optimization, N+1 prevention via specifications |

---

## Code Comparison

### Before: ProductService (In-Memory)

```csharp
public sealed class ProductService : IProductService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ProductModel> _products;

    public ProductService()
    {
        var products = new[]
        {
            new ProductModel { Id = Guid.NewGuid(), Name = "Wireless Mouse", ... },
            // ... hardcoded seed data ...
        };
        _products = products.ToDictionary(product => product.Id);
    }

    public IReadOnlyList<ProductResponse> GetAll()
    {
        lock (_sync)
        {
            return _products.Values.Select(ToResponse).ToArray();
        }
    }

    public ProductResponse? GetById(Guid id)
    {
        lock (_sync)
        {
            return _products.TryGetValue(id, out var product) ? ToResponse(product) : null;
        }
    }

    public ProductResponse Create(CreateProductRequest request)
    {
        var product = new ProductModel
        {
            Name = request.Name,
            // ... mapping ...
        };

        lock (_sync)
        {
            _products.Add(product.Id, product);
            return ToResponse(product);
        }
    }

    // Update, Delete, ToResponse helpers...
}
```

**Issues:**
- Manual lock management
- Data lost on restart
- Hard to search/filter
- No query patterns
- Difficult to test

---

### After: ProductService (Database-Backed)

```csharp
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var products = await _repository.GetAllAsync(cancellationToken);
        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product != null ? ToResponse(product) : null;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            // ... mapping ...
        };

        var created = await _repository.AddAsync(product, cancellationToken);
        return ToResponse(created);
    }

    // Update, Delete, ToResponse helpers...
}
```

**Advantages:**
- No lock management (handled by EF Core)
- Data persisted automatically
- Repository abstracts data access
- Easy to inject mock repository for testing
- Can add complex queries without changing service

---

### New: Advanced Query Example

**Before: In-Memory (Manual LINQ)**
```csharp
public IReadOnlyList<ProductResponse> SearchProducts(string searchTerm)
{
    lock (_sync)
    {
        return _products.Values
            .Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                     || p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Take(10)
            .Select(ToResponse)
            .ToList();
    }
}
```

**After: With Specification Pattern + Query Filter Helper**
```csharp
public async Task<(IReadOnlyList<ProductResponse> Items, int Total)> SearchProductsAsync(
    string searchTerm,
    int pageNumber = 1,
    int pageSize = 10,
    CancellationToken cancellationToken = default)
{
    var spec = new ProductSearchWithPaginationSpec(searchTerm, pageNumber, pageSize);
    var products = await _repository.FindAsync(spec, cancellationToken);
    var total = await _repository.CountAsync(new ProductSearchSpec(searchTerm), cancellationToken);
    
    return (products.Select(ToResponse).ToList(), total);
}
```

**Benefits:**
- Query logic encapsulated in `ProductSearchWithPaginationSpec`
- Strongly-typed (Expression<Func<>>)
- Translates to efficient SQL
- Reusable across multiple services
- Easy to test in isolation
- No N+1 queries (includes handled by specification)

---

## Repository Structure Comparison

### Before: No Repository Pattern
```csharp
// Service directly manages data structure
public class ProductService
{
    private Dictionary<Guid, Product> _products = new();
}
```

### After: Repository Pattern with Dependency Injection
```csharp
// Service depends on abstraction
public class ProductService
{
    private readonly IProductRepository _repository;
    
    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }
}

// Generic repository (EF Core implementation)
public class Repository<T> : IRepository<T> where T : class, IAuditableEntity
{
    private readonly EShopDbContext _context;
    
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }
    
    public async Task<IReadOnlyList<T>> FindAsync(ISpecification<T> spec, CancellationToken cancellationToken)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }
}

// Specific repository for advanced queries
public class ProductRepository : Repository<Product>, IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetActiveByCategoryAsync(
        string category,
        CancellationToken cancellationToken)
    {
        var spec = new ProductByCategorySpec(category);
        return await FindAsync(spec, cancellationToken);
    }
}
```

---

## Testing Comparison

### Before: Hard to Mock
```csharp
[Fact]
public void GetProductById_ReturnsProduct()
{
    // Can't mock ProductService storage directly
    var service = new ProductService();
    // Must rely on seeded data
    var result = service.GetById(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    Assert.NotNull(result);
}
```

### After: Easy to Mock & Test

**Unit Test with Mocked Repository:**
```csharp
[Fact]
public async Task GetProductByIdAsync_ReturnsProduct_WhenExists()
{
    // Arrange: Mock repository
    var mockRepository = new Mock<IProductRepository>();
    var product = new Product { Id = Guid.NewGuid(), Name = "Test Product" };
    mockRepository
        .Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(product);
    
    var service = new ProductService(mockRepository.Object);

    // Act
    var result = await service.GetByIdAsync(product.Id, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Product", result.Name);
    mockRepository.Verify(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
}
```

**Integration Test with Real Database:**
```csharp
[Fact]
public async Task GetProductByIdAsync_ReturnsProduct_WithRealDatabase()
{
    // Arrange: Use in-memory SQLite
    var options = new DbContextOptionsBuilder<EShopDbContext>()
        .UseSqlite("Data Source=:memory:")
        .Options;
    
    using var context = new EShopDbContext(options);
    await context.Database.EnsureCreatedAsync();
    
    var product = new Product { Id = Guid.NewGuid(), Name = "Test Product", Price = 29.99m };
    await context.Products.AddAsync(product);
    await context.SaveChangesAsync();
    
    var repository = new ProductRepository(context);
    var service = new ProductService(repository);

    // Act
    var result = await service.GetByIdAsync(product.Id, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Product", result.Name);
}
```

---

## Data Migration Path

### Step 1: Export Existing Data
```csharp
// ProductService current state
var products = service.GetAll(); // 5 seeded products
// IDs: 10000000-0000-0000-0000-000000000001 through 005
```

### Step 2: Create Database & Seed via OnModelCreating
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Seed with same IDs and data
    modelBuilder.Entity<Product>().HasData(
        new Product
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with adjustable DPI.",
            SKU = "ACC-MOU-001",
            Price = 29.99m,
            StockQuantity = 45,
            Category = "Accessories",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        },
        // ... 4 more products
    );
}
```

### Step 3: No Code Changes Needed
- Endpoints return same DTOs
- Response format identical
- Clients don't know storage changed

### Result
- **Backward Compatible**: Existing API clients see no difference
- **Zero Data Loss**: All products/orders preserved with same IDs
- **Transparent Upgrade**: App users don't need to do anything

---

## Performance Characteristics

| Operation | Before (In-Memory) | After (SQLite) | Notes |
|-----------|-------------------|----------------|-------|
| Get all products | O(n) in-memory | SQL query w/ index | DB benefits from indexes |
| Get by ID | O(1) dict lookup | SQL PK lookup | Similar performance |
| Search text | O(n) LINQ-to-Objects | SQL LIKE w/ index | Much faster for large datasets |
| Filter + Sort | O(n log n) in-memory | SQL order by | DB does all work |
| Pagination | O(n) all then slice | SQL LIMIT/OFFSET | Only fetches needed rows |
| Concurrent writes | Lock contention | ACID transactions | Safe concurrent access |
| Data persistence | None (lost) | 100% durable | Full durability |

---

## Migration Checklist

- [ ] Create `EShop.Infrastructure` project
- [ ] Add EF Core NuGet packages
- [ ] Create `EShopDbContext` with DbSets
- [ ] Create entity configurations (Fluent API)
- [ ] Create initial migration with seed data
- [ ] Implement `IRepository<T>` and generic `Repository<T>`
- [ ] Implement specific repositories (Product, Order, OrderItem)
- [ ] Implement `IUnitOfWork`
- [ ] Create specifications for common queries
- [ ] Implement `QueryFilterHelper`
- [ ] Refactor `ProductService` to use `IProductRepository`
- [ ] Refactor `OrderService` to use `IOrderRepository`
- [ ] Update `Program.cs` to register Infrastructure services
- [ ] Configure Aspire AppHost for database
- [ ] Create comprehensive tests
- [ ] Verify all endpoints still work
- [ ] Run `dotnet build` and `dotnet test`
- [ ] Update CHANGELOG-AI.md
- [ ] Delete in-memory implementation

---

## File Changes Summary

### Files to Create (30+)
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
│   └── *Filter.cs (5-7 filters)
├── Configurations/
│   ├── ProductEntityConfiguration.cs
│   ├── OrderEntityConfiguration.cs
│   └── OrderItemEntityConfiguration.cs
└── ServiceCollectionExtensions.cs
```

### Files to Modify (5-7)
```
src/EShop.ApiService/
├── EShop.ApiService.csproj
├── Program.cs
├── Services/ProductService.cs
└── Services/OrderService.cs

src/EShop.AppHost/
├── AppHost.cs
└── EShop.AppHost.csproj

src/EShop.sln
```

### Files to Delete
```
# No files deleted; in-memory implementation refactored to use repositories
```

---

## Key Metrics

| Metric | Before | After |
|--------|--------|-------|
| **Persistence** | ❌ None | ✅ Full |
| **Scalability** | ❌ Single instance | ✅ Multi-instance |
| **Data Consistency** | ❌ No ACID | ✅ ACID transactions |
| **Query Patterns** | ❌ Limited | ✅ Advanced (specs, filters) |
| **Testing** | ❌ Hard to mock | ✅ Easy to mock/test |
| **Audit Trail** | ❌ None | ✅ CreatedAt/UpdatedAt |
| **N+1 Queries** | N/A (in-memory) | ✅ Prevented via specs |
| **Production Ready** | ❌ No | ✅ Yes |

---

## Conclusion

The transition from in-memory to database-backed storage with Clean Architecture provides:

1. **Reliability**: Data persists across restarts
2. **Scalability**: Multiple app instances can share data
3. **Maintainability**: Clear separation of concerns via layered architecture
4. **Testability**: Repositories make unit testing trivial
5. **Extensibility**: Specifications and filters support future complex queries
6. **Performance**: SQL indexes, query optimization, and N+1 prevention
7. **Enterprise-Ready**: ACID transactions, audit trails, and proper concurrency handling

This plan positions EShopCopilot for production deployment and future growth.
