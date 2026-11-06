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
