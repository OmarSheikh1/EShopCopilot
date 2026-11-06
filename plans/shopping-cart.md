# Shopping Cart Feature Plan

## 1. Feature overview

Allow a shopper to browse active products, add them to a cart, change quantities,
remove lines, and submit checkout details. A successful checkout creates an
`Order` through the existing `OrderService`; the cart is cleared only after the
API confirms that order creation succeeded.

**Change name:** Shopping Cart and Checkout  
**Proposed Change ID:** `CHG-20261106-001-SHOPPING-CART`

The exact Change ID was searched in the repository and is not currently present.
Use it consistently for implementation comments and the implementation changelog.

## 2. Repository findings and design decisions

- The solution targets .NET 10 and uses Aspire, Minimal APIs, and Interactive
  Server Blazor.
- `EShop.Web` currently has only the starter Home page and navigation; it has no
  API clients, cart state, or test project.
- The AppHost already references the API service from the Web project. Service
  defaults configure HttpClient service discovery and resilience.
- Products can be read from `GET /api/products/`. The existing `POST /api/orders/`
  endpoint delegates order creation to `IOrderService`/`OrderService`.
- `Order` requires customer name and email and includes product item snapshots.
- Cart state will live in a **scoped Web service** so each interactive Blazor
  circuit has its own cart. Do not store carts in an API singleton: there is no
  authenticated shopper or cart identity in the current solution.
- Checkout will call an order-creation API that routes to the existing
  `OrderService`. The API, not the browser, must supply authoritative product
  names and prices to the resulting order.
- Initial cart state is in-memory for the lifetime of the Blazor circuit. Refresh,
  reconnect, or server restart persistence is not included in this iteration.
- No stock decrement/reservation requirement is defined. The implementation
  should not silently add inventory mutation; whether checkout must reserve or
  decrement stock is an open product decision. Until decided, this plan treats
  stock reservation as out of scope and records overselling as a risk.

## 3. Architecture and data flow

1. The cart page loads product DTOs using a typed `ProductApiClient` backed by an
   `HttpClient` configured for the Aspire `apiservice` service-discovery name.
2. The shopper adds a product or changes/removes a line. A scoped `CartState`
   stores product IDs and quantities, and raises a change notification for the
   cart page, product listing, and navigation badge.
3. Cart display uses current product details from the product API for names and
   prices. The cart total is calculated from decimal prices.
4. Checkout validates customer name/email and a non-empty cart in the UI, then
   sends customer details and product IDs/quantities to the API. It does not send
   trusted prices or product names.
5. A checkout-capable Minimal API operation resolves current product data through
   `IProductService`, validates product availability and requested quantities,
   builds the order item snapshots, and creates the order through the existing
   `IOrderService`/`OrderService`.
6. On a successful `201 Created` response, the Blazor page clears `CartState` and
   navigates to a confirmation view containing the returned order ID. On
   validation, service, or network failure, the cart remains intact and an
   actionable error is shown.

If checkout is implemented as a dedicated `POST /api/orders/checkout`, preserve
the current generic order endpoint for existing callers. The checkout service
operation must reuse `OrderService` rather than maintaining a second order store.
The Web client must treat only a successful response with a valid order result
as success.

## 4. Exact files

### Backend

**Create**

- `src/EShop.ApiService/Contracts/CheckoutOrderRequest.cs` — checkout request
  containing customer name/email and item product IDs/quantities only.

**Modify**

- `src/EShop.ApiService/Contracts/IOrderService.cs` — add a checkout-oriented
  operation that can create orders from product IDs/quantities.
- `src/EShop.ApiService/Services/OrderService.cs` — resolve authoritative product
  details and construct the order through the existing service.
- `src/EShop.ApiService/Endpoints/OrderEndpoints.cs` — map the checkout route
  with typed results, validation metadata, and OpenAPI descriptions.
- `src/EShop.ApiService/Program.cs` — preserve existing registrations and wire
  any required service dependencies.
- `src/EShop.ApiService/EShop.ApiService.http` — add successful and invalid
  checkout examples.

`src/EShop.ApiService/Contracts/ProductContracts.cs` and
`src/EShop.ApiService/Services/ProductService.cs` are read-only integration
dependencies unless implementation determines a domain rule requires a narrowly
scoped product-service change. Do not change the existing product model or order
model without a demonstrated requirement.

### Blazor Web

**Create**

- `src/EShop.Web/Contracts/CartLine.cs` — UI cart-line state (product ID and
  quantity; no independent source of truth for price).
- `src/EShop.Web/Services/CartState.cs` — scoped cart operations, snapshots,
  derived count/total, and change notification.
- `src/EShop.Web/Services/ProductApiClient.cs` — typed product catalog client
  for list/by-ID calls as needed.
- `src/EShop.Web/Services/OrderApiClient.cs` — typed checkout client; propagates
  cancellation and reports non-success responses rather than hiding them.
- `src/EShop.Web/Components/Pages/Products.razor` — product listing and add-to-cart
  interaction.
- `src/EShop.Web/Components/Pages/Cart.razor` — line editing/removal, totals,
  checkout form, and load/error/empty/submission states.
- `src/EShop.Web/Components/Pages/OrderConfirmation.razor` — confirmation after
  successful order creation.
- `src/EShop.Web/Components/Shared/CartSummary.razor` — reusable count/total
  display for navigation and cart contexts.
- `src/EShop.Web/Contracts/CheckoutOrderRequest.cs` and
  `src/EShop.Web/Contracts/CheckoutOrderResponse.cs` — Web-side transport
  contracts, unless the codebase later adopts a shared contracts project.

**Modify**

- `src/EShop.Web/Program.cs` — configure typed clients with the Aspire
  `apiservice` discovery URI and register `CartState` as scoped.
- `src/EShop.Web/Components/_Imports.razor` — import relevant Web namespaces.
- `src/EShop.Web/Components/Layout/NavMenu.razor` — add Products and Cart links
  and display a live cart count.
- `src/EShop.Web/Components/Pages/Home.razor` — replace the starter content with
  links or a concise entry point to shopping.
- `src/EShop.AppHost/AppHost.cs` — only if the existing `WithReference(apiService)`
  wiring is insufficient; avoid duplicate service configuration.

### Tests and tracking

**Create**

- `tests/EShop.ApiService.Tests/EShop.ApiService.Tests.csproj` and focused tests
  for request validation, checkout service rules, and endpoint response behavior.
- `tests/EShop.Web.Tests/EShop.Web.Tests.csproj` and component/state tests for cart
  interactions and checkout state transitions.
- `CHANGELOG-AI.md` — add the implementation entry with the proposed Change ID,
  scope, files, architecture decisions, and verification results.

The repository currently has no test project. Select test frameworks and
component-test tooling during implementation based on the solution's supported
.NET 10 package versions; do not add dependencies during this planning-only task.

## 5. Ordered implementation steps

1. Confirm the open decisions below, especially stock reservation and cart
   persistence. Retain the proposed Change ID for the entire implementation.
2. Define checkout request/response contracts and validation rules. Checkout
   must reject missing customer fields, empty carts, empty product IDs, and
   quantities below one.
3. Add a checkout operation in the existing order service path. Resolve every
   product on the server, reject missing/inactive products, and construct order
   items using the server's product name and price. Keep order persistence in
   `OrderService`; do not accept client totals or prices.
4. Add the checkout Minimal API route with documented typed results. Use `201`
   plus `Location` on success, validation problem details for invalid input, and
   clear not-found/conflict behavior for invalid product or stock state.
5. Add Web transport contracts and typed API clients using service discovery.
   Propagate cancellation tokens and surface API/network failures to the caller.
6. Implement `CartState` as a scoped service with one line per product ID,
   quantity merging on repeated add, quantity bounds, safe immutable snapshots,
   derived totals, and change notifications.
7. Build the Products page and reusable cart summary. Handle loading, empty,
   unavailable-product, and API-error states; disable adding inactive products.
8. Build the Cart page and checkout form. Prevent duplicate submissions while
   checkout is running. Clear state only after confirmed order creation; retain
   it for validation, conflict, server, malformed-response, or transport errors.
9. Add navigation and an order-confirmation page; verify route handling for
   browser refresh where applicable and the documented non-persistent cart
   behavior.
10. Add service, API, state, and component tests; update `.http` examples and
    `CHANGELOG-AI.md`.
11. Build the solution, run all tests, and launch the Aspire AppHost to verify
    service discovery and the complete checkout workflow.

## 6. Validation and business rules

- Product IDs must be non-empty and refer to active products.
- Quantity must be a positive integer; a repeat add increments the existing line
  rather than creating duplicate product lines.
- Removing the final line produces the empty-cart state.
- Totals use `decimal`, are derived from current product prices, and are not
  accepted from the browser as authoritative.
- Checkout request requires customer name, valid email, and at least one valid
  line. Invalid checkout must not create an order or clear cart state.
- Order item name and unit price are snapshots resolved by the backend at
  checkout time.
- Cart page recalculates its display when product data refreshes; checkout
  revalidation remains authoritative.
- API errors use the existing Problem Details pipeline and appropriate 400/404/
  409 responses. The UI presents safe, user-facing errors without swallowing
  exceptions or displaying internal details.
- Order status on creation remains the existing default (`Pending`); the browser
  cannot choose order status.

## 7. Testing strategy

### Unit tests

- Cart state: add, merge duplicate product, set quantity, reject zero/negative
  quantities, remove, clear, count, total, notifications, and isolation between
  separate scoped instances.
- Checkout/order service: empty request, invalid product, inactive product,
  invalid quantity, server-priced snapshot, successful creation through the
  existing `OrderService`, and no order for rejected input.
- API client: success mapping, Problem Details/non-success mapping, malformed
  response, and cancellation propagation.

### API integration tests

- `POST /api/orders/checkout` returns `201` and a `Location` header with an
  order response for valid input.
- Invalid customer/line data returns `400` Problem Details.
- Missing/inactive product returns the chosen documented client error and does
  not create an order.
- Returned order line values and totals are server-derived.

### Blazor/component tests

- Products render loading, error, empty, and success states; add action updates
  the cart count.
- Cart allows quantity changes and removal, displays the computed total, and
  validates checkout fields.
- Successful checkout clears the cart and displays/navigates to confirmation.
- Failed, cancelled, or malformed checkout leaves the cart unchanged and permits
  retry.
- Separate circuit-scoped state instances do not leak cart contents.

### End-to-end

- Run AppHost and exercise product listing -> add multiple products -> change a
  quantity -> remove a line -> checkout -> verify created order and empty cart.
- Exercise API failure and invalid product cases; verify cart retention and no
  false confirmation.
- Confirm OpenAPI includes checkout metadata and Aspire discovery targets the
  API service without a hardcoded host/port.

## 8. Risks, dependencies, and open decisions

- **Stock policy:** The request does not say whether successful checkout must
  decrement/reserve stock. With no reservation, two shoppers can order the last
  unit concurrently. If required, define a concurrency-safe inventory operation
  and failure/rollback semantics before implementation.
- **Cart persistence:** Scoped server-side state is isolated per circuit but is
  lost on refresh/reconnect/server restart. Decide whether local storage or
  durable backend carts are required before promising persistence.
- **Customer identity:** No authentication or customer profile exists. Checkout
  therefore collects customer name/email directly; it must not treat these
  values as authenticated identity.
- **Network ambiguity:** If the server creates an order but the response is lost,
  retrying may create a duplicate order. Consider an idempotency key if retries
  must be safe.
- **In-memory data:** Products and orders are currently in-memory; restart loses
  them. This feature follows that existing convention and does not add a
  database.
- **Checkout payload shape:** A dedicated checkout endpoint keeps existing
  order-creation callers compatible, but duplicates some surface area. Confirm
  whether reusing `POST /api/orders/` with a checkout-safe request shape is
  preferred.

## 9. Acceptance criteria

- A shopper can browse active products, add products, update quantities, and
  remove items.
- Cart count and decimal total update immediately and consistently.
- Cart state is isolated per interactive circuit.
- Checkout requires valid customer information and a non-empty cart.
- The backend validates product IDs and quantities, derives product snapshots
  from server data, and creates the order through the existing `OrderService`.
- Only confirmed successful order creation clears the cart and shows an order
  confirmation; all failures preserve cart contents.
- UI and API show loading, empty, validation, and error states accessibly.
- Aspire service discovery is used rather than hardcoded API ports.
- API, cart-state, component, and end-to-end tests cover success and failure
  paths; solution build and test suites pass.
- Implementation adds the Change ID consistently and records results in
  `CHANGELOG-AI.md`.
