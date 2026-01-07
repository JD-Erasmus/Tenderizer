# Testing Strategy (V1)

This document describes the testing approach for the Tenderizer V1 codebase.

## Goals

- Provide fast feedback for core business rules (CRUD + reminder scheduling).
- Keep tests deterministic and isolated from external infrastructure.
- Focus on boundaries and rules that are easy to regress:
  - authorization constraints (owner vs admin)
  - time-based logic (UTC, reminder offsets)
  - idempotency (no duplicate reminders)

## Test Levels

### 1. Unit tests (primary for V1)

**Project:** `TenderizerTest`

Unit tests exercise application services directly:

- `Tenderizer.Services.TenderService`
- `Tenderizer.Services.ReminderScheduler`

These tests use:

- EF Core **InMemory** provider for persistence (`Microsoft.EntityFrameworkCore.InMemory`)
- in-memory `IConfiguration` for override scenarios (e.g. `ReminderOffsetsMinutes`)

Rationale:

- The services are where V1 business rules live.
- The InMemory provider gives high speed and isolation.

> Note: EF Core InMemory is not a relational store and does not enforce all relational behaviors (e.g., SQL translation differences, unique constraints). For V1 this is acceptable because we validate the service logic and behavior. If relational behavior becomes important, move to SQLite in-memory.

### 2. Integration tests (optional, later)

Not required for V1, but recommended as a follow-up:

- Use SQLite in-memory or SQL Server LocalDB container to validate:
  - migrations
  - indexes/constraints
  - `ExecuteDeleteAsync` translation

### 3. End-to-end tests (optional, later)

Not required for V1.

Potential future coverage:

- UI flows for tender CRUD (Razor Pages / MVC views)
- background worker sending reminders

## Coverage Focus

### `TenderService` test coverage

Key behaviors tested:

- **Create**
  - trims string inputs
  - non-admin always becomes owner (ignores `OwnerUserId` in payload)
  - admin can set `OwnerUserId`
  - `CreatedAtUtc` and `UpdatedAtUtc` are set
  - validation: non-admin cannot create non-terminal tenders with closing time in the past

- **Update**
  - authorization: only owner or admin
  - non-admin cannot reassign owner
  - updates fields and bumps `UpdatedAtUtc`

- **GetDetails**
  - authorization enforced

- **Delete**
  - removes the entity

### `ReminderScheduler` test coverage

Key behaviors tested:

- `ClearPendingAsync` deletes only unsent reminders (`SentAtUtc == null`)
- `RegenerateAsync`
  - clears pending reminders first
  - terminal status: clears pending and creates nothing
  - active status: creates reminders based on offsets
  - **Option B:** skips reminders that would be in the past
  - supports config override via `ReminderOffsetsMinutes`
  - idempotent behavior when called repeatedly (no duplicates)

## Deterministic Testing (time)

Services use `DateTimeOffset.UtcNow` directly.

Test strategy:

- Set `ClosingAtUtc` relative to the current time (e.g., `UtcNow.AddDays(5)`) so the expected behavior is stable.
- Avoid asserting exact timestamps for audit fields (just validate ordering / presence).

If stricter determinism becomes necessary, a follow-up improvement is to introduce a `TimeProvider` abstraction and inject it into services.

## How to Run

Run only the Tenderizer tests:

- `dotnet test TenderizerTest/TenderizerTest.csproj`

(If you have a larger solution, running `dotnet test` at the repo root may execute unrelated projects.)

## Naming and Organization

- Test classes map 1:1 with the services under test.
  - `TenderServiceTests`
  - `ReminderSchedulerTests`
- Test methods follow: `{MethodUnderTest}_{Scenario}_{ExpectedOutcome}`

## What is intentionally not tested in V1

- SMTP / email delivery mechanics
- Background worker loop behavior (will be covered once implemented)
- Razor UI rendering and client-side validation
