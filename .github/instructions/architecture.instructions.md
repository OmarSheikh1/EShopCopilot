---
applyTo: "src/**/*.cs"
---

# EShopCopilot Architecture Standards

- Follow Vertical Slice Architecture.
- Organize features by business capability where practical.
- Keep Minimal API endpoints thin.
- Place business rules and state transitions in services or domain logic.
- Do not put business logic in HTTP endpoint handlers.
- Follow the Single Responsibility Principle.
- Use dependency injection instead of directly constructing services.
- Avoid unnecessary coupling between domains.
- Prefer asynchronous methods for I/O operations.
- Propagate CancellationToken through asynchronous operations.
- Use DTOs for API request and response contracts.
- Avoid exposing persistence entities directly.
- Keep architecture simple; do not introduce unnecessary abstractions.
- Ensure business operations are independently testable.
- Follow existing project conventions before introducing new patterns.

## Architecture Review

Check for:
- Business logic leaking into Minimal API endpoints.
- Tight coupling between services.
- Incorrect dependency injection lifetimes.
- Missing input validation.
- Unsafe shared mutable state.
- Missing cancellation or error handling.
- Unnecessary dependencies or abstractions.
- Violations of the Single Responsibility Principle.