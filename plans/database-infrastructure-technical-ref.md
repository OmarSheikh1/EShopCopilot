# Database Infrastructure: Technical Reference

**Change ID**: CHG-20261106-003-DATABASE-INFRASTRUCTURE

---

## Quick Reference: Core Abstractions

### IRepository<T>
```csharp
public interface IRepository<T> where T : class, IAuditableEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
```

### ISpecification<T>
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

### IUnitOfWork
```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IOrderItemRepository OrderItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Entity Models

### Product (from existing Model)
```
ID (Guid, PK) | Name | Description | SKU | Price | StockQuantity | Category | IsActive | CreatedAt | UpdatedAt | RowVersion
```

**Indexes:**
- PK: Id
- Index: Category, IsActive
- Index: Name (for search)

### Order (from existing Model)
```
ID (Guid, PK) | CustomerName | CustomerEmail | CreatedAt | UpdatedAt | Status | RowVersion
```

**Indexes:**
- PK: Id
- Index: CustomerEmail
- Index: CreatedAt
- Index: Status

### OrderItem (from existing Model)
```
ID (Guid, PK) | OrderId (FK) | ProductId | ProductName | Quantity | UnitPrice | CreatedAt | UpdatedAt
```

**Indexes:**
- PK: Id
- FK: OrderId → Order (cascade delete)

---

## Specification Examples

### ProductSearchSpec
```csharp
public class ProductSearchSpec : Specification<Product>
{
    public ProductSearchSpec(string searchTerm)
    {
        var term = searchTerm.ToLower();
        Criteria = p => p.IsActive &&
                        (p.Name.ToLower().Contains(term) ||
                         p.Description.ToLower().Contains(term));
        OrderBy = p => p.Name;
    }
}
```

### OrderByCustomerEmailSpec
```csharp
public class OrderByCustomerEmailSpec : Specification<Order>
{
    public OrderByCustomerEmailSpec(string email)
    {
        Criteria = o => o.CustomerEmail.ToLower() == email.ToLower();
        Includes.Add(o => o.Items);
        OrderByDescending = o => o.CreatedAt;
    }
}
```

---

## Query Filter Helper Usage

### Example 1: Product Search with Pagination
```csharp
var filter = new QueryFilterHelper<Product>()
    .AddTextSearch("mouse", p => p.Name, p => p.Description)
    .AddFilter(p => p.IsActive)
    .AddFilter(p => p.Category == "Accessories")
    .OrderBy(p => p.Price, ascending: true)
    .Paginate(pageNumber: 1, pageSize: 10);

var spec = filter.BuildSpecification();
var products = await _productRepository.FindAsync(spec, cancellationToken);
```

### Example 2: Order History with Date Range
```csharp
var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
var filter = new QueryFilterHelper<Order>()
    .AddFilter(o => o.CustomerEmail == email)
    .AddDateRange(o => o.CreatedAt, thirtyDaysAgo, DateTime.UtcNow)
    .OrderBy(o => o.CreatedAt, ascending: false)
    .Paginate(pageNumber: 1, pageSize: 20);

var spec = filter.BuildSpecification();
var orders = await _orderRepository.FindAsync(spec, cancellationToken);
```

---

## Service Refactoring Pattern

### Before
```csharp
public sealed class ProductService : IProductService
{
    private readonly Dictionary<Guid, Product> _products = new();
    
    public IReadOnlyList<ProductResponse> GetAll()
    {
        lock (_sync) { return _products.Values.Select(ToResponse).ToList(); }
    }
}
```

### After
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
}
```

**Key Changes:**
1. Inject `IProductRepository` (abstraction, not concrete storage)
2. Remove lock/sync concerns (EF Core handles concurrency)
3. Make methods async with CancellationToken
4. Return same DTOs (backward compatible)

---

## DI Registration (Program.cs)

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=data.db;Cache=Shared";

builder.Services.AddEShopInfrastructure(connectionString);

// Keep existing service registrations
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

---

## Testing Templates

### Unit Test (Mocked Repository)
```csharp
[Fact]
public async Task GetByIdAsync_ReturnsProduct_WhenFound()
{
    // Arrange
    var productId = Guid.NewGuid();
    var product = new Product { Id = productId, Name = "Test" };
    
    var mockRepo = new Mock<IProductRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(product);
    
    var service = new ProductService(mockRepo.Object);

    // Act
    var result = await service.GetByIdAsync(productId, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

### Integration Test (Real DB)
```csharp
[Fact]
public async Task GetByIdAsync_ReturnsProduct_WithRealDatabase()
{
    // Arrange
    var options = new DbContextOptionsBuilder<EShopDbContext>()
        .UseSqlite("Data Source=:memory:")
        .Options;
    
    using var context = new EShopDbContext(options);
    await context.Database.EnsureCreatedAsync();
    
    var product = new Product { Id = Guid.NewGuid(), Name = "Test", Price = 10.0m };
    await context.Products.AddAsync(product);
    await context.SaveChangesAsync();

    // Act
    var result = await context.Products.FindAsync(product.Id);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}
```

---

## Migration File Structure

### Initial Migration: 20261106000001_InitialCreate

```
20261106000001_InitialCreate.cs
├─ Up() method
│  ├─ CreateTable("Products")
│  ├─ CreateTable("Orders")
│  ├─ CreateTable("OrderItems")
│  ├─ CreateIndex on Category, IsActive
│  ├─ CreateIndex on CustomerEmail
│  └─ InsertData (seed products/orders)
└─ Down() method (reverse operations)

20261106000001_InitialCreate.Designer.cs
└─ Generated by dotnet ef migrations

EShopDbContextModelSnapshot.cs
└─ Current model snapshot for future migrations
```

---

## Aspire AppHost Configuration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder
    .AddProject<Projects.EShop_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ConnectionStrings__DefaultConnection", 
        "Data Source=/data/eshop.db;Cache=Shared");

builder
    .AddProject<Projects.EShop_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// Volume mounting for database persistence
builder.AddVolume("eshop-data", "/data");

builder.Build().Run();
```

---

## Concurrency Handling

### Optimistic Concurrency (RowVersion)
```csharp
public class Product : IAuditableEntity
{
    public Guid Id { get; set; }
    // ... other properties
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}

// EF Core automatically:
// 1. Includes RowVersion in UPDATE WHERE clause
// 2. Throws DbUpdateConcurrencyException if not matched
// 3. Returns updated RowVersion after save
```

### Handling Concurrency Exception
```csharp
try
{
    var product = await _repository.GetByIdAsync(id, cancellationToken);
    product.Price = newPrice;
    await _repository.UpdateAsync(product, cancellationToken);
}
catch (DbUpdateConcurrencyException ex)
{
    // Log conflict
    // Retry or return conflict error to client
    return TypedResults.Conflict("Product was modified by another user");
}
```

### Transaction Example
```csharp
using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    // Multiple operations within transaction
    var order = new Order { /* ... */ };
    await _unitOfWork.Orders.AddAsync(order, cancellationToken);
    
    var orderItem = new OrderItem { OrderId = order.Id, /* ... */ };
    await _unitOfWork.OrderItems.AddAsync(orderItem, cancellationToken);
    
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

---

## Common Queries

### 1. Get Active Products by Category with Pagination
```csharp
var spec = new ProductByCategorySpec("Accessories")
    .WithPagination(pageNumber: 1, pageSize: 20);
var products = await _productRepository.FindAsync(spec, cancellationToken);
```

### 2. Search Products with Text Search
```csharp
var spec = new ProductSearchSpec("wireless")
    .WithPagination(pageNumber: 1, pageSize: 10);
var products = await _productRepository.FindAsync(spec, cancellationToken);
```

### 3. Get Order History for Customer
```csharp
var spec = new OrderByCustomerEmailSpec("customer@example.com");
var orders = await _orderRepository.FindAsync(spec, cancellationToken);
```

### 4. Get Recent Orders
```csharp
var spec = new OrdersByDateRangeSpec(
    DateTime.UtcNow.AddDays(-30),
    DateTime.UtcNow);
var orders = await _orderRepository.FindAsync(spec, cancellationToken);
```

### 5. Get Orders by Status
```csharp
var spec = new OrdersByStatusSpec(OrderStatus.Confirmed);
var orders = await _orderRepository.FindAsync(spec, cancellationToken);
```

---

## Error Handling

| Exception | Scenario | Handling |
|-----------|----------|----------|
| `DbUpdateException` | Constraint violation (FK, unique) | Log, return 400 Bad Request |
| `DbUpdateConcurrencyException` | RowVersion mismatch | Log, return 409 Conflict |
| `InvalidOperationException` | No DbSet for type | Verify DbContext configuration |
| `SqliteException` | Database locked | Implement retry logic |
| `DbUpdateConcurrencyException` | Cascade delete violation | Check related entities |

---

## Performance Optimization Checklist

- [ ] Add indexes on frequently-filtered columns (Category, CustomerEmail, Status)
- [ ] Use includes in specifications to prevent N+1 queries
- [ ] Enable query caching for read-only queries
- [ ] Profile queries with EF Core logs in development
- [ ] Avoid select N+1 by eagerly loading related entities
- [ ] Use pagination for large result sets
- [ ] Implement database connection pooling
- [ ] Monitor query execution times
- [ ] Consider materialized views for complex aggregations

---

## Rollback Plan

1. **Immediate Rollback**: Delete `data.db` file; app creates new clean database on startup
2. **Keep In-Memory as Fallback**: Comment out Infrastructure DI; revert services to in-memory (1 hour effort)
3. **Partial Rollback**: Revert specific services while others use database
4. **Git Revert**: `git revert CHG-20261106-003-...` (if changes committed)

---

## Validation Commands

```bash
# Build solution
dotnet build src\EShop.sln

# Add migration (from Infrastructure project folder)
dotnet ef migrations add InitialCreate --project EShop.Infrastructure

# Update database
dotnet ef database update --project EShop.Infrastructure

# Run all tests
dotnet test src\EShop.sln

# Check for vulnerabilities
dotnet list package --vulnerable

# Run Aspire AppHost
dotnet run --project src\EShop.AppHost
```

---

## Expected Test Coverage

| Component | Target Coverage | Count |
|-----------|-----------------|-------|
| Repositories | 80% | 15-20 tests |
| Specifications | 85% | 12-15 tests |
| Query Filters | 80% | 10-12 tests |
| Services | 75% | 15-20 tests |
| DbContext | 70% | 8-10 tests |
| **Total** | **~75%** | **60-77 tests** |

---

## Deployment Checklist

- [ ] Database initialized on app startup
- [ ] Connection string configured via Aspire
- [ ] Health check endpoint reports database status
- [ ] Data directory has proper permissions (read/write)
- [ ] Database file backed up before production deployment
- [ ] Monitoring/alerts set up for database errors
- [ ] Connection pooling tuned for expected load
- [ ] Query performance baseline established
- [ ] Rollback procedure documented and tested

---

**Document Version**: 1.0  
**Last Updated**: 2026-11-06  
**Status**: Ready for Implementation
