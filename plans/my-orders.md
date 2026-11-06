# Plan: "My Orders" navigation link and order history page

**Change Name:** MY-ORDERS
**Proposed Change ID:** `CHG-20261106-002-MY-ORDERS` (verify it is unused in `CHANGELOG-AI.md` before implementing; one ID for the whole feature)
**Status:** PLAN ONLY. Awaiting approval.

## 1. Feature overview

Add a **My Orders** link to the sidebar. Clicking it opens `/my-orders`, which lists the customer's orders with status, date, items and total. Each row links to the existing `/orders/{id}` page.

### Key finding: there is no authentication
The repo has no auth or user identity. `Order` only has `CustomerName` and `CustomerEmail`, and checkout collects both in [Cart.razor](../src/EShop.Web/Components/Pages/Cart.razor). Also, `GET /api/orders` returns **every** customer's orders, so the web app must not call it and filter client-side.

**Proposed approach (needs your confirmation):** identify "my" orders by customer email.
- The page remembers the email used at checkout (circuit-scoped state) and loads that customer's orders automatically.
- Otherwise it shows an email lookup form.
- Real security (an authenticated user) is out of scope. The privacy limitation is listed in the risks below.

## 2. Architecture and data flow

```
NavMenu "My Orders" -> /my-orders (MyOrders.razor, interactive server)
  -> CustomerSession (scoped; holds last checkout email, if any)
  -> OrderApiClient.GetOrdersByEmailAsync(email)
  -> GET https+http://apiservice/api/orders?customerEmail=x@y.com   (Aspire service discovery)
  -> OrderEndpoints (thin) -> IOrderService.GetByCustomerEmail(email)
  -> in-memory Dictionary<Guid, Order>, filtered case-insensitively, newest first
  -> List<OrderResponse> (Status serialized as string by the existing JsonStringEnumConverter)
```

## 3. Backend (EShop.ApiService)

Reuse the existing `OrderResponse`, `OrderItemResponse` and the `OrderStatus` enum (Pending, Confirmed, Shipped, Delivered, Cancelled). No new models or DTOs are needed.

- **`GET /api/orders?customerEmail=`**: extend the existing `GetOrders` endpoint with an optional query parameter.
  - Absent: unchanged behavior (returns all orders), so existing tests and `.http` examples keep working.
  - Present: validate it with `[EmailAddress]` and a length of at most 320. Invalid input returns a 400 validation problem.
  - Present and valid: return only that customer's orders. An unknown email returns 200 with an empty list (not 404, so we don't reveal whether an account exists).
- **Single order by ID and email:** extend the existing `GET /api/orders/{id}` with an optional `customerEmail` query parameter.
  - Present: the order is returned only if its ID and email (trimmed, case-insensitive) both match. A mismatch returns **404**, the same as a missing ID, so it doesn't reveal that the order exists. An invalid email returns 400.
  - Absent: unchanged behavior, so existing tests keep working.
  - Service: add `OrderResponse? GetByIdAndEmail(Guid id, string email)` to `IOrderService` and implement it under the `_sync` lock.
  - The web app always sends the email.
- **Alternative (cleaner separation):** a dedicated `GET /api/orders/by-customer?email=`. Choose one during approval; the default is the query parameter above.
- **Service:** add `IReadOnlyList<OrderResponse> GetByCustomerEmail(string email)` to `IOrderService` and implement it in `OrderService` under the existing `_sync` lock. It matches `CustomerEmail` with `StringComparison.OrdinalIgnoreCase` after trimming and sorts by `CreatedAt` descending. Business logic stays in the service, per the architecture rules.
- **DI:** no change. `IOrderService` is already registered as a singleton.
- **Security:** do not log emails. Do not expose other customers' data when the filter is supplied.

## 4. Blazor frontend (EShop.Web)

- **Navigation:** in [NavMenu.razor](../src/EShop.Web/Components/Layout/NavMenu.razor), add a `NavLink href="my-orders"` with a `bi bi-bag-check` icon between Products and the cart summary.
- **Page:** `Components/Pages/MyOrders.razor` at `@page "/my-orders"`. It reuses the same `.alert`, `.table` and `.badge` styles as the Cart page.
  - Shows a loading state, an error state (safe message, retry button), an empty state ("No orders yet" with a Continue shopping link) and the orders list.
  - Each order shows a short ID, created date, status badge, item lines (name x qty), total, and a link to `/my-orders/{id}`.
  - Status badge colors: Pending = secondary, Confirmed = primary, Shipped = info, Delivered = success, Cancelled = danger.
- **Single order view:** `Components/Pages/OrderDetails.razor` at `@page "/my-orders/{OrderId:guid}"`.
  - It takes the email from `CustomerSession`, or from an `?email=` query parameter, or from an inline email prompt when neither exists.
  - It calls `OrderApiClient.GetOrderAsync(id, email)` and shows the items, status, dates and total.
  - A 404 shows "Order not found for this email".
  - The list rows on `/my-orders` link here. The existing `/orders/{id}` confirmation page stays unchanged.
- **Email lookup form:** uses `EditForm` with `DataAnnotations` validation (`[Required, EmailAddress, StringLength(320)]`), the same pattern as the Cart `CheckoutForm`. It is shown when no remembered email exists and also lets the user change it.
- **State:** new scoped `Services/CustomerSession.cs` that stores the last checkout email. `Cart.razor` sets it after a successful checkout (small modification). Register it in `Program.cs` as scoped, next to `CartState`.
- **Typed API client:** add `OrderApiClient.GetOrdersByEmailAsync(string email, CancellationToken)`.
  - It uses `Uri.EscapeDataString`.
  - It requires a 200 response and throws `ApiClientException.ForOrdersResponse(status)` otherwise.
  - It validates the JSON payload, as `CheckoutAsync` does.
- **Contracts:** `Contracts/OrderSummary.cs`, or reuse `CheckoutOrderResponse` (it has the same shape). Recommended: add a neutral `OrderSummary` / `OrderItemSummary` rather than reuse the checkout-named type.
- **Interaction:** the page cancels in-flight requests on dispose and via a `CancellationTokenSource` when the email changes.

## 5. Files

**Create**
- `src/EShop.Web/Components/Pages/MyOrders.razor`
- `src/EShop.Web/Components/Pages/OrderDetails.razor`
- `src/EShop.Web/Services/CustomerSession.cs`
- `src/EShop.Web/Contracts/OrderSummary.cs`
- `tests/EShop.Web.Tests/MyOrdersComponentTests.cs`
- `tests/EShop.Web.Tests/CustomerSessionTests.cs`
- `tests/EShop.ApiService.Tests/OrderHistoryEndpointTests.cs`

**Modify**
- `src/EShop.ApiService/Contracts/IOrderService.cs` (new method)
- `src/EShop.ApiService/Services/OrderService.cs` (implementation)
- `src/EShop.ApiService/Endpoints/OrderEndpoints.cs` (optional `customerEmail` filter and validation)
- `src/EShop.ApiService/EShop.ApiService.http` (filter examples)
- `src/EShop.Web/Services/OrderApiClient.cs` and `ApiClientException.cs`
- `src/EShop.Web/Components/Layout/NavMenu.razor`
- `src/EShop.Web/Components/Pages/Cart.razor` (remember email after success)
- `src/EShop.Web/Program.cs` (register `CustomerSession`)
- `CHANGELOG-AI.md` (entry for `CHG-20261106-002-MY-ORDERS`)

All new or changed code carries `CHG-20261106-002-MY-ORDERS` comments per the change-tracking standard.

## 6. Ordered implementation steps

1. Confirm the Change ID and the email-based identification approach.
2. Backend service method, interface and endpoint filter with validation.
3. API tests and `.http` examples. Build ApiService.
4. Web contracts, `OrderApiClient` method and `ApiClientException` mapping.
5. `CustomerSession`, DI registration, and the `Cart.razor` hook.
6. `MyOrders.razor` and the NavMenu link.
7. Web component tests.
8. Build the solution and run all tests. Smoke-test via AppHost (checkout, then My Orders shows the new order).
9. Update `CHANGELOG-AI.md`.

## 7. Dependencies and risks

- **No auth, so email lookup is not private.** Anyone who knows an email can see its orders. Mitigation now: return only a minimal necessary payload, and don't reveal existence (empty list). Long term: add authentication and derive the customer from the identity. This is a decision for you.
- Orders are in-memory, so history is lost on API restart. Seed orders use example emails, and that is the data a tester will see for those emails.
- `GET /api/orders` still returns all orders unauthenticated (pre-existing). It is not fixed here, but the web app will not use it.
- The remembered email lives in the circuit, so a page refresh or new circuit falls back to the lookup form.
- Status strings in the web contract depend on the API's `JsonStringEnumConverter`. Tests should cover it.

## 8. Acceptance criteria

- The sidebar shows "My Orders" and navigates to `/my-orders`, with an active highlight.
- After checkout, "My Orders" lists the new order (status Pending) without re-entering the email.
- Entering an email shows only that email's orders, newest first, each with a status, date, items and total.
- An invalid email shows a validation message and makes no API call.
- `GET /api/orders/{id}?customerEmail=` returns the order only when both ID and email match. A wrong email or unknown ID returns 404, and an invalid email returns 400.
- `/my-orders/{id}` shows a single order's details, or a not-found message when the ID and email don't match.
- Loading, empty and error states render properly.
- `GET /api/orders` without the filter behaves as before. With it, the response is filtered, case-insensitive and 400 on invalid email.
- The solution builds, all tests pass, and `CHANGELOG-AI.md` is updated.

## 9. Testing strategy

- **Unit (service):** case-insensitive match, trimming, ordering, an unknown email returning an empty list, and no cross-customer leakage.
- **Single order:** ID and email match returns 200 (case-insensitive); right ID with another customer's email returns 404; unknown ID returns 404; invalid email returns 400; no email behaves as before. bUnit covers `OrderDetails` found, not-found and missing-email prompt.
- **API integration:** no filter returns all orders; a valid filter returns the subset; an invalid email returns 400; a checkout followed by a filter query includes the new order.
- **Blazor (bUnit):** the nav link renders; the page shows loading, then orders with status badges; empty state; error state on a 500; invalid email blocks the call; a remembered email auto-loads.
- **End-to-end scenario:** add a product to the cart, check out as a given email, open My Orders, and see the Pending order. Then update the order status via `PUT /api/orders/{id}`, reload, and see the new status.
