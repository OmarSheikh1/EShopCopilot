# AI-Assisted Change Log

## CHG-20261106-001-SHOPPING-CART — Shopping Cart and Checkout

- **Date:** 2026-11-06
- **Purpose:** Add product browsing, circuit-scoped cart editing, and checkout that creates an order through the existing `OrderService`.
- **Prompt:** `/implement-feature Shopping Cart : attached is the approved plan`
- **Plan:** [plans/shopping-cart.md](plans/shopping-cart.md)

### Files created

- `src/EShop.ApiService/Contracts/CheckoutOrderRequest.cs`
- `src/EShop.Web/Components/Pages/Cart.razor`
- `src/EShop.Web/Components/Pages/OrderConfirmation.razor`
- `src/EShop.Web/Components/Pages/Products.razor`
- `src/EShop.Web/Components/Shared/CartSummary.razor`
- `src/EShop.Web/Contracts/CartLine.cs`
- `src/EShop.Web/Contracts/CheckoutOrderRequest.cs`
- `src/EShop.Web/Contracts/CheckoutOrderResponse.cs`
- `src/EShop.Web/Contracts/ProductSummary.cs`
- `src/EShop.Web/Services/ApiClientException.cs`
- `src/EShop.Web/Services/CartState.cs`
- `src/EShop.Web/Services/OrderApiClient.cs`
- `src/EShop.Web/Services/ProductApiClient.cs`
- `tests/EShop.ApiService.Tests/CheckoutEndpointTests.cs`
- `tests/EShop.ApiService.Tests/EShop.ApiService.Tests.csproj`
- `tests/EShop.Web.Tests/CartComponentTests.cs`
- `tests/EShop.Web.Tests/CartStateTests.cs`
- `tests/EShop.Web.Tests/EShop.Web.Tests.csproj`
- `tests/EShop.Web.Tests/ProductComponentTests.cs`

### Files modified

- `src/EShop.ApiService/Contracts/IOrderService.cs`
- `src/EShop.ApiService/Endpoints/OrderEndpoints.cs`
- `src/EShop.ApiService/EShop.ApiService.http`
- `src/EShop.ApiService/Program.cs`
- `src/EShop.ApiService/Services/OrderService.cs`
- `src/EShop.ServiceDefaults/Extensions.cs`
- `src/EShop.Web/Components/App.razor`
- `src/EShop.Web/Components/Layout/NavMenu.razor`
- `src/EShop.Web/Components/Pages/Home.razor`
- `src/EShop.Web/Components/_Imports.razor`
- `src/EShop.Web/Program.cs`
- `src/EShop.sln`

### Architectural decisions

- Added `POST /api/orders/checkout`; it accepts customer details and product IDs/quantities only, resolves current product names/prices server-side, validates active status and stock limits, and creates the order using `OrderService`.
- Cart contents are scoped to a Blazor circuit and stored in memory. Checkout clears the cart only after a `201 Created` response with a valid order ID; a version check preserves cart changes made while checkout was in flight.
- Aspire service discovery is used for typed API clients. The existing shared resilience policy now avoids retrying unsafe HTTP methods to prevent automatic replay of order-creating requests.
- Per plan, checkout does not reserve or decrement inventory. It validates requested quantities against current stock; durable cart/order storage and idempotency for user-initiated retries are out of scope.
- Added API integration tests and Web state/component tests. The Web test dependency uses AngleSharp 1.5.0 to avoid the advisory affecting earlier versions.

### Verification

- `dotnet test src\EShop.sln` — passed; 16 tests, 0 failures.
- `dotnet build src\EShop.sln --no-restore` — succeeded; 0 warnings, 0 errors.
- `dotnet list tests\EShop.Web.Tests\EShop.Web.Tests.csproj package --vulnerable --include-transitive` — no vulnerable packages reported.
- Aspire AppHost and the `apiservice` and `webfrontend` resources were started and reported healthy during smoke validation. The storefront rendered products and cart state through Aspire service discovery.

## CHG-20261106-002-MY-ORDERS — My Orders

- **Date:** 2026-11-06
- **Purpose:** Add a "My Orders" nav link, an email-based order history page with status, and single-order lookup by order ID and email.
- **Prompt:** `/implement-feature : implement the my orders feature`
- **Plan:** [plans/my-orders.md](plans/my-orders.md)

### Files created

- `src/EShop.Web/Components/Pages/MyOrders.razor`
- `src/EShop.Web/Components/Pages/OrderDetails.razor`
- `src/EShop.Web/Components/Shared/OrderStatusBadge.cs`
- `src/EShop.Web/Contracts/OrderSummary.cs`
- `src/EShop.Web/Services/CustomerSession.cs`
- `tests/EShop.ApiService.Tests/OrderHistoryEndpointTests.cs`
- `tests/EShop.Web.Tests/MyOrdersComponentTests.cs`

### Files modified

- `src/EShop.ApiService/Contracts/IOrderService.cs`
- `src/EShop.ApiService/Services/OrderService.cs`
- `src/EShop.ApiService/Endpoints/OrderEndpoints.cs`
- `src/EShop.ApiService/EShop.ApiService.http`
- `src/EShop.Web/Services/OrderApiClient.cs`
- `src/EShop.Web/Services/ApiClientException.cs`
- `src/EShop.Web/Components/Layout/NavMenu.razor`
- `src/EShop.Web/Components/Pages/Cart.razor`
- `src/EShop.Web/Program.cs`
- `tests/EShop.Web.Tests/CartComponentTests.cs`

### Architectural decisions

- Customers are identified by email (no authentication exists); this is not private, so real auth is a follow-up.
- Optional `customerEmail` query parameter on existing `GET /api/orders` and `GET /api/orders/{id}`; unfiltered behavior is unchanged.
- ID/email mismatch returns 404 (same as a missing ID); invalid email returns 400; unknown email returns an empty list.
- Matching logic lives in `OrderService` (case-insensitive, trimmed, newest first).
- Web remembers the checkout email in a scoped `CustomerSession`.

### Testing and build results

- `dotnet test src\EShop.sln`: build succeeded; 18 web tests and 13 API tests passed.

## CHG-20261106-003-DATABASE-INFRASTRUCTURE — EF Core and SQLite persistence

- **Date:** 2026-11-08
- **Purpose:** Replace transient product and order dictionaries with persistent SQLite storage while preserving API behavior and seeded IDs.
- **Prompt:** "implement the database operations as planned"
- **Plan:** [plans/README-DATABASE-INFRASTRUCTURE.md](plans/README-DATABASE-INFRASTRUCTURE.md)

### Files created

- `src/EShop.Infrastructure/EShop.Infrastructure.csproj`
- `src/EShop.Infrastructure/Data/EShopDbContext.cs`
- `src/EShop.Infrastructure/Data/EShopDbContextFactory.cs`
- `src/EShop.Infrastructure/Data/DatabaseHealthCheck.cs`
- `src/EShop.Infrastructure/Data/Configurations/ProductEntityConfiguration.cs`
- `src/EShop.Infrastructure/Data/Configurations/OrderEntityConfiguration.cs`
- `src/EShop.Infrastructure/Data/Configurations/OrderItemEntityConfiguration.cs`
- `src/EShop.Infrastructure/Data/Configurations/SeedData.cs`
- `src/EShop.Infrastructure/Data/Migrations/20261109044609_InitialCreate.cs`
- `src/EShop.Infrastructure/Data/Migrations/20261109044609_InitialCreate.Designer.cs`
- `src/EShop.Infrastructure/Data/Migrations/EShopDbContextModelSnapshot.cs`
- `src/EShop.Infrastructure/Models/IAuditableEntity.cs`
- `src/EShop.Infrastructure/Models/Product.cs`
- `src/EShop.Infrastructure/Models/Order.cs`
- `src/EShop.Infrastructure/Models/OrderItem.cs`
- `src/EShop.Infrastructure/Repositories/IRepository.cs`
- `src/EShop.Infrastructure/Repositories/Repository.cs`
- `src/EShop.Infrastructure/Repositories/IUnitOfWork.cs`
- `src/EShop.Infrastructure/Repositories/UnitOfWork.cs`
- `src/EShop.Infrastructure/Repositories/Product/IProductRepository.cs`
- `src/EShop.Infrastructure/Repositories/Product/ProductRepository.cs`
- `src/EShop.Infrastructure/Repositories/Order/IOrderRepository.cs`
- `src/EShop.Infrastructure/Repositories/Order/OrderRepository.cs`
- `src/EShop.Infrastructure/Repositories/OrderItem/IOrderItemRepository.cs`
- `src/EShop.Infrastructure/Repositories/OrderItem/OrderItemRepository.cs`
- `src/EShop.Infrastructure/Repositories/Specifications/ISpecification.cs`
- `src/EShop.Infrastructure/Repositories/Specifications/Specification.cs`
- `src/EShop.Infrastructure/Repositories/Specifications/ProductSpecifications.cs`
- `src/EShop.Infrastructure/Repositories/Specifications/OrderSpecifications.cs`
- `src/EShop.Infrastructure/Filters/FilterOptions.cs`
- `src/EShop.Infrastructure/Filters/QueryFilterHelper.cs`
- `src/EShop.Infrastructure/ServiceCollectionExtensions.cs`
- `src/EShop.ApiService/Middleware/DatabaseConcurrencyExceptionHandler.cs`
- `tests/EShop.ApiService.Tests/DatabaseTestApplicationFactory.cs`
- `tests/EShop.ApiService.Tests/DatabasePersistenceTests.cs`
- `MIGRATION.md`

### Files modified

- `.gitignore`
- `CHANGELOG-AI.md`
- `README.md`
- `src/EShop.sln`
- `src/EShop.AppHost/AppHost.cs`
- `src/EShop.ApiService/EShop.ApiService.csproj`
- `src/EShop.ApiService/Program.cs`
- `src/EShop.ApiService/appsettings.json`
- `src/EShop.ApiService/Contracts/IProductService.cs`
- `src/EShop.ApiService/Contracts/IOrderService.cs`
- `src/EShop.ApiService/Endpoints/ProductEndpoints.cs`
- `src/EShop.ApiService/Endpoints/OrderEndpoints.cs`
- `src/EShop.ApiService/Services/ProductService.cs`
- `src/EShop.ApiService/Services/OrderService.cs`
- `src/EShop.ApiService/EShop.ApiService.http`
- `src/EShop.ServiceDefaults/Extensions.cs`
- `tests/EShop.ApiService.Tests/CheckoutEndpointTests.cs`
- `tests/EShop.ApiService.Tests/OrderHistoryEndpointTests.cs`

### Files moved

- `src/EShop.ApiService/Models/Product.cs` → `src/EShop.Infrastructure/Models/Product.cs`
- `src/EShop.ApiService/Models/Order.cs` → `src/EShop.Infrastructure/Models/Order.cs`
- `src/EShop.ApiService/Models/OrderItem.cs` → `src/EShop.Infrastructure/Models/OrderItem.cs`

### Architectural decisions

- Added the EF Core SQLite Infrastructure project with migrations, repositories, specifications, a Unit of Work, query filters, and a database readiness check. Existing API DTOs and routes remain unchanged; service and endpoint calls are asynchronous and cancellation-aware.
- The initial migration seeds the existing five products and five orders with their stable IDs. SQLite files are resolved relative to the API content root by default; Aspire forwards the configurable connection string.
- Product and order concurrency use integer version tokens because SQLite has no auto-generated `rowversion`. Order items cascade with their parent; `ProductId` remains a historical reference rather than a foreign key so product deletion does not break past orders.
- Each integration test uses a separate SQLite file except the persistence test, which deliberately restarts the API against the same file.

### Testing and build results

- `dotnet test src\EShop.sln --no-restore` — passed; 34 tests, 0 failures.
- `dotnet build src\EShop.sln --no-restore` — succeeded; 0 warnings, 0 errors.
- EF migration snapshot check — no model changes pending.
- `dotnet list src\EShop.sln package --vulnerable --include-transitive` — no vulnerable packages reported.
- API integration tests verify CRUD, seed data, restart persistence, concurrency, health, OpenAPI, and SQL search/sort/paging.
