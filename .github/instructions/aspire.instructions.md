---
applyTo: "src/EShop.AppHost/**,src/EShop.ServiceDefaults/**,src/EShop.ApiService/Program.cs,src/EShop.Web/Program.cs"
---

# .NET Aspire Standards

## Framework

- Target the .NET version configured by the actual project files.
- Use modern C# language features supported by the configured SDK.
- Prefer collection expressions where they improve readability.

## Aspire AppHost

- Use Aspire AppHost for distributed application orchestration.
- Use `aspire run` when the installed Aspire CLI and repository configuration support it.
- Otherwise use the supported AppHost project launch command.
- Configure service references and dependencies through Aspire.
- Avoid hardcoded service URLs and ports.
- Do not place business logic inside AppHost.

## ServiceDefaults

- Preserve existing `AddServiceDefaults()` calls.
- Preserve OpenTelemetry configuration.
- Preserve health checks and service discovery.
- Follow existing HTTP resilience conventions.
- Avoid duplicating shared infrastructure configuration.

## Minimal APIs

- Organize related endpoints using `MapGroup()`.
- Keep endpoint handlers thin.
- Register domain services through dependency injection.
- Map domain endpoints consistently in `Program.cs`.
- Return appropriate HTTP status codes.

## Configuration and Security

- Do not hardcode connection strings or credentials.
- Use environment-specific configuration.
- Keep secrets outside source control.
- Follow established Aspire resource configuration conventions.

## Validation

- Build affected projects after changes.
- Verify AppHost resource references.
- Ensure API and Web services can communicate through the configured service discovery mechanism.
- Validate that health checks are correctly reporting service status.
- Confirm that OpenTelemetry traces are being generated and collected as expected.
- Verify that service discovery is functioning correctly for all services.
- Confirm that Aspire AppHost is correctly orchestrating service interactions.
- Validate that all configured service dependencies are reachable and functioning as expected.