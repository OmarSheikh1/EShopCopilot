---
name: create-domain
description: Implement complete CRUD for an existing domain model using the repository standards
argument-hint: Domain model name (for example Product)
---

Implement complete CRUD for the domain model named in the user's invocation: **${input:domainName:Product}**.

First read `.github/instructions/domain.instructions.md` and inspect the actual model and existing repository conventions. Apply every relevant standard in that file even if your current context does not automatically include it.

Then implement the domain end to end: service, Minimal API endpoints, dependency injection and endpoint mapping in `Program.cs`, and five HTTP request examples in the existing `.http` file. Use the real model properties and the project's actual .NET/C# versions.

If the named model does not exist, ask whether to create it and what properties it needs; do not guess a business schema.

Build the API project and run available relevant tests. Summarize the changed files and any verification failures. Do not merely generate a plan: make the code changes.
