---
name: implement-feature
description: Implement an approved feature plan end-to-end using repository architecture, coding, testing, and change-tracking standards.
argument-hint: Feature name or path to its approved plan (e.g. Shopping Cart)
---

# Implement an Approved Feature

Feature or approved plan: **${input:feature:Shopping Cart}**

## Mission

Implement the requested feature end-to-end, following the approved plan and the repository's established conventions. Do not invent a new architecture or replace working functionality.

## 1. Discover the plan and repository

1. Locate the approved plan for the supplied feature. Search `plans/`, `docs/`, the repository root, and existing planning files. If the user supplies a path, use that path.
2. If multiple plans match, ask which is approved. If no approved plan is found, stop and ask the user to run `/plan-feature` or provide the plan. Never silently implement an unapproved plan.
3. Read `.github/copilot-instructions.md` (if present) and applicable `.github/instructions/*.instructions.md`, especially architecture, domain, Aspire, Blazor, security, testing, and change tracking.
4. Inspect actual project names, project files, SDK versions, services, endpoints, API contracts, Blazor components, tests, and Aspire AppHost wiring. Treat the plan's file paths and assumptions as proposals until verified.
5. Identify dependencies, unresolved decisions, and any conflict between the plan and current code. Ask before making material architecture or business-rule decisions.

## 2. Implement according to established standards

- Follow the plan's ordered implementation steps, backend and frontend scope, and acceptance criteria.
- Preserve Vertical Slice boundaries, thin Minimal API endpoints, and business logic in appropriate services.
- Reuse existing domain services and contracts; do not create duplicate sources of truth.
- Follow the repository's actual persistence strategy and DI lifetimes.
- Apply server-side validation, authorization where required, safe error handling, cancellation, and concurrency safeguards.
- Use existing Aspire service discovery, `AddServiceDefaults()`, observability, and resource references rather than hardcoded service addresses.
- Implement Blazor pages, components, API clients, loading/error/empty states, accessibility, and state management as required by the approved plan.
- For multi-step operations, account for partial failures, retries, and idempotency as specified in the plan.
- Do not add packages, modify public contracts, or change persistence semantics without verifying the need and compatibility.

## 3. Change tracking

- Follow `.github/instructions/change-tracking.instructions.md` if present.
- Reuse the approved plan's Change ID; if absent, obtain or generate one according to repository rules before editing.
- Use the same Change ID for all related edits and the changelog entry.
- Add concise, meaningful change comments only around significant new or modified logical sections, with language-appropriate syntax. Avoid comment noise and invalid comments in JSON.
- Update `CHANGELOG-AI.md` with files changed, design decisions, and verification results.
- Never discard unrelated uncommitted changes or commit automatically.

## 4. Verification

1. Add or update unit, API integration, and Blazor component tests appropriate to the plan.
2. Add or update `.http` request examples where applicable.
3. Build affected projects and run relevant tests; run the full solution checks when practical.
4. Verify the feature's acceptance criteria and critical success/failure paths.
5. If tooling prevents a check, state precisely what was not verified; never claim tests passed without running them.

## 5. Final report

Report:
- Feature name and Change ID
- Plan used and deviations (with reasons)
- Files created and modified
- Key behavior implemented
- Build/test results and any failures
- Remaining risks or manual verification steps

**Execution mode:** Implement code, not just a plan. Respect IDE/tool approval requirements; never assume permission for destructive operations.
