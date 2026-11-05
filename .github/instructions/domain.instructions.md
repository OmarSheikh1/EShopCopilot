---
applyTo: "src/EShop.ApiService/**/*.cs,src/EShop.ApiService/**/*.http"
---

# Domain Implementation Standards — EShop.ApiService

## Discovery and scope
- Before editing, inspect the requested model in `src/EShop.ApiService/Models/`, `Program.cs`, existing endpoints/services, project file, and `.http` files.
- Use the existing model and its real properties; do not invent or silently change fields.
- Preserve repository conventions and use the project's configured .NET/C# versions.
- Implement domains inside `EShop.ApiService` unless the user explicitly requests a separate service.

## Naming and structure
- Keep each domain implementation cohesive, under `Features/<DomainName>/` where new files are needed.
- Create `<DomainName>Service.cs` for CRUD business logic.
- Create `<DomainName>Endpoints.cs` as a static class exposing `Map<DomainName>Endpoints(this IEndpointRouteBuilder app)`.
- Use existing namespaces and patterns where they conflict with this suggested structure; explain deviations.

## Service behavior
- Default to in-memory storage for a new CRUD domain unless the user requests database persistence or the repository already defines a persistence convention for that domain.
- Seed exactly five realistic records when requested or when creating a new in-memory CRUD domain.
- Implement GetAll, GetById, Create, Update, Delete with appropriate return types and clear not-found behavior.
- Register an in-memory service as a singleton to retain state for the application lifetime. Protect mutable state from concurrent requests using locking or concurrent data structures; never expose a mutable collection directly.
- Use dependency injection and keep business logic outside endpoint handlers.
- Do not introduce EF Core, MediatR, repositories, or new NuGet packages unless required by existing architecture or explicit requirements.

## Minimal APIs
- Group routes under `/api/<plural-domain-name>` (for Product, `/api/products`).
- Map GET collection, GET by id, POST, PUT by id, and DELETE by id.
- Use suitable typed results/status codes: GET 200, POST 201 with Location, PUT 200 or 204, DELETE 204, missing resource 404, invalid request 400 or validation problem.
- Do not expose mutable storage objects directly; use DTOs when needed for safe input/output contracts.
- Provide endpoint names/OpenAPI metadata where supported by the project.
- Follow existing authentication/authorization conventions; never make a protected operation anonymous.

## Validation and safety
- Validate required fields, IDs, and domain invariants before mutations.
- Reject invalid requests without modifying state.
- Do not log credentials, secrets, or sensitive request payloads.
- Use appropriate CancellationToken propagation for asynchronous I/O; don't create artificial async methods for in-memory operations.

## Registration and examples
- Service class should be put in services folder should implement an contract(in Contracts folder) and be registered in Program.cs as a singleton.
- Endpoints class should be put in endpoints folder and should be registered in Program.cs.
- Update `src/EShop.ApiService/Program.cs` to register the service and map its endpoints, preserving existing registrations.
- Update `src/EShop.ApiService/EShop.ApiService.http` (or the existing API .http file) with working GET all, GET by id, POST, PUT, DELETE examples using real model properties.
- Add or update relevant tests if a test project exists; otherwise report that automated tests are not configured rather than inventing a project without approval.
- Build the affected project and run relevant tests when tools are available; report failures truthfully.
