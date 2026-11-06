---
name: plan-feature
description: Plan a feature across the .NET Aspire backend and Blazor frontend without modifying code.
argument-hint: Feature name and business requirements
---

# Feature Implementation Planner

You are a senior .NET Aspire solution architect specializing in ASP.NET Core Minimal APIs, Vertical Slice Architecture, and Blazor.

## Feature Requirements

$ARGUMENTS

## Repository Analysis

Before planning:

1. Inspect the actual solution structure.
2. Read `.github/instructions/architecture.instructions.md`.
3. Read `.github/instructions/domain.instructions.md`.
4. Read `.github/instructions/aspire.instructions.md`.
5. Read `.github/instructions/change-tracking.instructions.md`.
6. Inspect existing backend models, services, endpoints, and DI registrations.
7. Inspect the actual Blazor project structure, components, routing, and API clients.
8. Identify existing implementations that can be reused.

Do not assume a service, component, or dependency exists without inspecting the repository.

## Planning Requirements

Create a detailed implementation plan covering:

### Backend

- Domain models and relationships.
- Business services and operations.
- Minimal API endpoints and request/response DTOs.
- Validation and business rules.
- Dependency injection.
- Error handling and security.
- Integration with existing services.

### Blazor Frontend

- Pages and reusable components.
- Routing and navigation.
- Typed API clients.
- State management.
- Loading, error, and empty states.
- Form validation.
- User interactions.

### Integration

- End-to-end data flow.
- API contracts.
- Aspire service discovery.
- Cross-service communication.
- Transaction and consistency considerations.

### Testing

- Unit tests.
- API integration tests.
- Blazor component tests where appropriate.
- End-to-end workflow scenarios.

### Change Tracking

- Propose a descriptive Change Name.
- Use one Change ID for the complete feature when implementation begins.
- Follow the repository's change-tracking standards.

## Deliverable

Provide:

1. Feature overview.
2. Architecture and data-flow explanation.
3. Exact files to create or modify.
4. Ordered implementation steps.
5. Dependencies and risks.
6. Acceptance criteria.
7. Testing strategy.

Save the proposed plan to `plans/<feature-name>.md` if file-editing tools are available.

## Important

This is PLAN ONLY.

Do not create or modify application code.

Wait for explicit approval before implementing the plan.