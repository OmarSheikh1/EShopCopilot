# Database Infrastructure Plan — Executive Summary

**Feature**: Database Infrastructure with EF Core + SQLite  
**Change ID**: CHG-20261106-003-DATABASE-INFRASTRUCTURE  
**Status**: Planning Complete ✓  
**Created**: 2026-11-06

---

## Vision

Transform EShopCopilot from in-memory storage to a production-grade, persistent database infrastructure using **Entity Framework Core (EF Core)**, **SQLite**, and **Clean Architecture** patterns. Maintain backward compatibility while enabling advanced querying, scalability, and enterprise features.

---

## What Gets Built

### 1. New Infrastructure Project (`EShop.Infrastructure`)
A complete data access layer implementing:
- **EF Core DbContext** with Product, Order, OrderItem DbSets
- **Generic Repository<T>** for standard CRUD operations
- **Specific Repositories** (ProductRepository, OrderRepository, OrderItemRepository)
- **Unit of Work Pattern** for transaction coordination
- **Specification Pattern** for type-safe, reusable query encapsulation
- **Query Filter Helper** for advanced filtering, search, sort, pagination, and date-range queries
- **Entity Configurations** using Fluent API
- **Migrations** to create and version database schema

### 2. Service Refactoring
- Refactor `ProductService` to use `IProductRepository` (abstraction)
- Refactor `OrderService` to use `IOrderRepository` (abstraction)
- Remove in-memory Dictionary storage
- Eliminate manual lock management
- Add async/await patterns throughout

### 3. Database
- **SQLite 3.x** file-based relational database
- Three main tables: Products, Orders, OrderItems
- Strategic indexes on frequently-filtered columns
- Seed data from existing in-memory collections
- Optimistic concurrency control via RowVersion

### 4. Aspire Integration
- Configure AppHost to manage SQLite connection
- Database health checks
- Connection string management
- Volume mounting for data persistence

---

## Architecture Diagram

```
┌─────────────────────────────────┐
│   Blazor Web (UI)               │
│   Pages, Components, Router     │
└────────────┬────────────────────┘
             │ HTTP/REST
┌────────────▼────────────────────┐
│   ASP.NET Core API (Minimal)    │
│   Endpoints, DTOs, Contracts    │
└────────────┬────────────────────┘
             │ Services depend on
┌────────────▼────────────────────┐
│   Application Layer (Services)  │
│   ProductService, OrderService  │
│   Repository Interfaces         │
└────────────┬────────────────────┘
             │ Abstractions
┌────────────▼──────────────────────────────┐
│   Infrastructure Layer (EF Core)          │
│   ├─ Generic Repository<T>                │
│   ├─ Specific Repositories                │
│   ├─ Unit of Work                         │
│   ├─ Specifications                       │
│   └─ Query Filter Helper                  │
└────────────┬──────────────────────────────┘
             │ Uses EF Core Provider
┌────────────▼──────────────────────────────┐
│   SQLite Database (Persistent)            │
│   ├─ Products Table                       │
│   ├─ Orders Table                         │
│   └─ OrderItems Table                     │
└─────────────────────────────────────────────┘
```

---

## Key Features

### ✅ Persistent Storage
- Data survives application restarts
- SQLite file-based storage (single file: `data.db`)
- Automatic seeding on first run

### ✅ Scalability
- Multiple application instances can share same database
- Connection pooling for efficient resource use
- Ready to scale to PostgreSQL in production

### ✅ Clean Architecture
- Clear separation of concerns (Presentation → Application → Infrastructure → Database)
- Dependency Inversion (services depend on abstractions, not implementations)
- Testable components via mocking and dependency injection

### ✅ Advanced Querying
- **Filtering**: Property-based criteria (e.g., Category = "Accessories")
- **Text Search**: Full-text search across Name, Description
- **Sorting**: Multi-column sort with ascending/descending
- **Pagination**: Page number / page size patterns
- **Date Ranges**: CreatedAt/UpdatedAt filtering
- **Status/Category**: Enum and categorical filters

### ✅ Type Safety
- Strongly-typed query expressions using `Expression<Func<T, TResult>>`
- No string-based dynamic LINQ
- Compile-time verification of query properties
- SQL translation verified at build time

### ✅ Transaction Support
- ACID guarantees via SQLite transactions
- Optimistic concurrency (RowVersion)
- Pessimistic locking for critical operations
- Transaction rollback on errors

### ✅ Audit Trail
- `CreatedAt` timestamp on all entities
- `UpdatedAt` timestamp automatically maintained
- Future extensibility for full audit logging

### ✅ No IQueryable Leakage
- Repositories never expose `IQueryable<T>`
- Only `IReadOnlyList<T>` returned to callers
- Ensures query logic stays in Infrastructure layer

### ✅ Backward Compatible
- Existing API endpoints unchanged
- Response DTOs identical to current format
- No breaking changes for API consumers
- Transparent to frontend (Blazor client sees no difference)

---

## Implementation Approach

### Phase 1: Foundation (2-3 hours)
1. Create `EShop.Infrastructure` project structure
2. Add EF Core NuGet packages
3. Implement base interfaces and abstract classes

### Phase 2: Database (2-3 hours)
4. Create `EShopDbContext` with DbSets
5. Implement entity configurations (Fluent API)
6. Create initial migration with seed data

### Phase 3: Data Access (3-4 hours)
7. Implement generic `Repository<T>`
8. Implement specific repositories
9. Create `IUnitOfWork` and implementation

### Phase 4: Queries (2-3 hours)
10. Implement specifications (12-15 different ones)
11. Create `QueryFilterHelper` and filter classes

### Phase 5: Integration (1 hour)
12. Create `ServiceCollectionExtensions` for DI
13. Register in `Program.cs`
14. Wire up Aspire AppHost

### Phase 6: Service Migration (2-3 hours)
15. Refactor `ProductService` to use repository
16. Refactor `OrderService` to use repository
17. Remove in-memory storage

### Phase 7: Testing (4-6 hours)
18. Unit tests for repositories
19. Unit tests for specifications
20. Integration tests with real database
21. Endpoint-to-database flow tests

### Phase 8: Validation (1-2 hours)
22. Build and test full solution
23. Aspire AppHost smoke test
24. Verify all endpoints work

**Total Estimated Effort**: 20-27 hours (3-4 days at 8 hours/day)

---

## Files Delivered

### 30+ New Files
```
EShop.Infrastructure/
├── Project file (csproj)
├── Data/
│   ├── DbContext
│   ├── Migrations (3 files)
│   └── Configurations (3 files)
├── Repositories/
│   ├── Generic repo interface & implementation
│   ├── Unit of Work interface & implementation
│   └── 3 specific repository pairs (6 files)
├── Specifications/
│   ├── Base specification
│   └── 7-8 specific specifications
├── Filters/
│   ├── QueryFilterHelper
│   └── 5-7 filter helpers
└── DI Registration file
```

### 5-7 Modified Files
```
EShop.ApiService/
├── EShop.ApiService.csproj (add Infrastructure reference)
├── Program.cs (add Infrastructure DI)
├── Services/ProductService.cs (refactor to use repository)
└── Services/OrderService.cs (refactor to use repository)

EShop.AppHost/
├── AppHost.cs (configure database)
└── EShop.AppHost.csproj (update if needed)

Solution file (add project reference)
```

### Documentation Files (Included)
- `database-infrastructure.md` — Complete detailed plan
- `database-infrastructure-before-after.md` — Transition guide
- `database-infrastructure-technical-ref.md` — Developer reference

---

## Quality Assurance

### Testing Strategy
- **Unit Tests**: 60-77 tests covering repositories, specifications, filters (≥75% coverage)
- **Integration Tests**: Real database tests using in-memory SQLite
- **Endpoint Tests**: Full request/response cycle verification
- **Build Validation**: `dotnet build src\EShop.sln` with zero warnings

### Acceptance Criteria
✓ All existing endpoints work identically  
✓ Data persists across restarts  
✓ No in-memory storage remaining  
✓ All tests pass (unit + integration)  
✓ No NuGet vulnerabilities  
✓ Aspire AppHost starts and reports healthy  
✓ Production-ready ACID transactions  
✓ ≥75% code coverage on Infrastructure layer  

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Data migration issues | Low | High | Seed data from existing services; verify before deployment |
| Performance regression | Medium | High | Add indexes; profile with EF Core logs |
| Breaking existing endpoints | Low | High | Comprehensive integration tests; verify DTOs unchanged |
| Concurrency conflicts | Medium | Medium | Optimistic locking (RowVersion); test race conditions |

---

## Success Metrics

1. ✓ **Persistence**: All data survives application restart
2. ✓ **Backward Compatibility**: All existing endpoints return identical responses
3. ✓ **Scalability**: Multiple app instances can run against same database
4. ✓ **Testability**: ≥80% coverage on Infrastructure layer
5. ✓ **Performance**: Query response times < 100ms for typical operations
6. ✓ **Build Quality**: Zero warnings, all tests passing
7. ✓ **Production Ready**: ACID transactions, audit trails, proper concurrency handling

---

## Related Documentation

- **Detailed Plan**: [database-infrastructure.md](./database-infrastructure.md)
- **Before/After Guide**: [database-infrastructure-before-after.md](./database-infrastructure-before-after.md)
- **Technical Reference**: [database-infrastructure-technical-ref.md](./database-infrastructure-technical-ref.md)
- **Repository Instructions**: [.github/instructions/domain.instructions.md](../.github/instructions/domain.instructions.md)
- **Architecture Standards**: [.github/instructions/architecture.instructions.md](../.github/instructions/architecture.instructions.md)

---

## Next Steps

### For Review
1. Share this plan with team stakeholders
2. Validate approach against enterprise requirements
3. Confirm timeline and resource availability
4. Get approval to proceed with implementation

### Upon Approval
1. Create and structure `EShop.Infrastructure` project
2. Implement DbContext and migrations
3. Build repository and unit of work pattern
4. Refactor existing services
5. Create comprehensive tests
6. Update documentation and changelog

---

## Frequently Asked Questions

**Q: Will this break existing API clients?**  
A: No. All DTOs and response formats remain identical. The persistence layer is transparent.

**Q: What about my existing data?**  
A: Existing products and orders are seeded into the database during initial migration. No data is lost.

**Q: Can I still run locally?**  
A: Yes. SQLite is file-based (no separate server). It creates a `data.db` file in the app directory.

**Q: How do I revert if something goes wrong?**  
A: Delete the `data.db` file. The app recreates it on next startup. Or revert Git commits.

**Q: Will queries be as fast as in-memory?**  
A: For small datasets, in-memory is faster. For 1,000+ records, database with indexes is significantly faster. Specifications prevent N+1 queries.

**Q: Can this scale to PostgreSQL later?**  
A: Yes. EF Core abstracts the database provider. Changing to PostgreSQL requires only configuration changes.

**Q: Do I need to understand all the patterns?**  
A: No. The plan includes code examples for common tasks. Patterns like Repository, Specification, and Query Filter are documented with usage examples.

---

## Change Tracking

**Change ID**: `CHG-20261106-003-DATABASE-INFRASTRUCTURE`  
**Status**: ✓ Planning Complete  
**Awaiting**: Approval to proceed with implementation  

---

**Plan Prepared By**: Copilot Code Architect  
**Date**: 2026-11-06  
**Version**: 1.0  

---

## Approval Sign-Off

- [ ] Architecture approved
- [ ] Timeline confirmed
- [ ] Resources allocated
- [ ] Dependencies validated
- [ ] Risk mitigation accepted

**Ready to proceed with implementation** ✓
