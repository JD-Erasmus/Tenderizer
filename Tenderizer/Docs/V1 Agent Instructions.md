# Tender Deadline Tracker (Internal) � V1 Agent Instructions (.NET 8 ASP.NET Core MVC)

## Objective
Replace the current whiteboard tender tracking with a web-based internal tool that:
- Captures tenders, ownership, and deadlines
- Surfaces due soon items on a dashboard
- Sends automatic deadline reminders via email
- Requires minimal training and mirrors the current workflow

## V1 Scope
### Must-have
- Authenticated access (company internal users)
- Tender CRUD
- Owner assignment (single accountable owner, with support from additional employees)
- Status updates
- Dashboard views (Urgent / This Week / Active / History)
- Automated reminder emails at fixed intervals
- Basic audit timestamps (CreatedAt/UpdatedAt)

### Non-goals (explicitly out of scope for V1)
- Kanban/drag-drop board
- Document uploads/attachments
- Comments/chat
- Tender scraping from portals
- Custom reminder schedules UI
- Multi-tenant SaaS mode

---

## Tech Stack
- .NET 8
- ASP.NET Core MVC + Razor Views
- EF Core (provider-agnostic; SQL Server)
- ASP.NET Core Identity (cookie auth)
- BackgroundService for reminders (no Hangfire in V1)

---

## Data & Time Rules
- Store datetimes as `DateTimeOffset` in UTC in DB.
- UI input/output uses UTC; display with an explicit `UTC` label.
- No local time conversion in V1 to avoid OS-specific timezone handling.

---

## Domain Model

### TenderStatus (enum)
Fixed set. No custom statuses.
- Draft
- Identified
- InProgress
- Submitted
- Won
- Lost
- Cancelled

### Entities

#### Tender
- `Guid Id`
- `string Name` (required, max 200)
- `string? ReferenceNumber` (max 100)
- `string? Client` (max 200)
- `string? Category` (max 100)
- `DateTimeOffset ClosingAtUtc` (required)
- `TenderStatus Status` (required)
- `string OwnerUserId` (required, FK to Identity user)
- `DateTimeOffset CreatedAtUtc`
- `DateTimeOffset UpdatedAtUtc`

Indexes:
- `ClosingAtUtc`
- `Status`
- `OwnerUserId`

#### TenderReminder
- `Guid Id`
- `Guid TenderId` (FK)
- `DateTimeOffset ReminderAtUtc`
- `DateTimeOffset? SentAtUtc`
- `int AttemptCount` (default 0)
- `string? LastError` (max 500)
- `DateTimeOffset CreatedAtUtc`

Indexes:
- `(SentAtUtc, ReminderAtUtc)` for fast pending lookup
- Unique constraint: `(TenderId, ReminderAtUtc)` to prevent duplicates

---

## Reminder Policy (Fixed for V1)
Generate reminders relative to ClosingAt:
- T-7 days
- T-3 days
- T-24 hours

Rules:
- Only generate reminders when status is in: `Identified`, `InProgress`, `Draft`
- Do not send reminders for: `Submitted`, `Won`, `Lost`, `Cancelled`
- If ClosingAt changes, delete unsent reminders and regenerate.
- If status becomes terminal, delete unsent reminders.

Idempotency:
- Enforce uniqueness on `(TenderId, ReminderAtUtc)` and always �upsert� behavior.

---

## Authentication & Authorization
- Use ASP.NET Core Identity.
- Roles:
  - `Admin`: full CRUD for all tenders, manage users (optional v1), reassign ownership
  - `User`: create tenders, edit tenders they own, view all tenders

Authorization rules:
- Any authenticated user can view dashboard + tender list.
- Only Admin can delete tenders.
- Only Owner or Admin can edit a tender.

Seed:
- Ensure role seeding runs on startup.
- Ensure at least one Admin exists (seed via config on first run).

---

## MVC UI Pages (Razor)

### Dashboard (`/`)
Sections (server-rendered):
1. **Urgent**: Closing in next 48h, not terminal
2. **This Week**: Closing in next 7 days, not terminal
3. **Active**: Status in Draft/Identified/InProgress, closing > 7 days
4. **History**: Submitted/Won/Lost/Cancelled (last 90 days by UpdatedAtUtc)

Sorting:
- Urgent/This Week by `ClosingAtUtc` ascending
- History by `UpdatedAtUtc` descending

### Tenders
- `/tenders` list with quick filters (optional: status dropdown)
- `/tenders/create`
- `/tenders/{id}`
- `/tenders/{id}/edit`
- `/tenders/{id}/delete` (Admin only)

UI requirements:
- Inline status dropdown on details/edit
- Owner dropdown (Admin always; User only on create if allowed else self)
- Date + time picker for closing datetime (UTC)

Validation:
- Name required
- Closing datetime must be in the future for non-terminal statuses (allow past only for terminal/history edits by Admin if needed)

---

## Controller Contracts

### TendersController
Actions:
- `Index()`
- `Details(Guid id)`
- `Create()` (GET)
- `Create(TenderUpsertVm vm)` (POST)
- `Edit(Guid id)` (GET)
- `Edit(Guid id, TenderUpsertVm vm)` (POST)
- `Delete(Guid id)` (GET, Admin)
- `DeleteConfirmed(Guid id)` (POST, Admin)

### HomeController
- `Index()` dashboard

ViewModels:
- `TenderListItemVm`
- `TenderDetailsVm`
- `TenderUpsertVm` (strongly validated; contains ClosingAtUtc)

---

## Application Services (Keep Controllers Thin)

### ITenderService
- `Task<IReadOnlyList<TenderListItemVm>> GetDashboardAsync(...)`
- `Task<TenderDetailsVm> GetDetailsAsync(Guid id, string userId, bool isAdmin)`
- `Task<Guid> CreateAsync(TenderUpsertDto dto, string userId, bool isAdmin)`
- `Task UpdateAsync(Guid id, TenderUpsertDto dto, string userId, bool isAdmin)`
- `Task DeleteAsync(Guid id)` (Admin only)

### IReminderScheduler
- `Task RegenerateAsync(Guid tenderId)` (called on create/update/status change)
- `Task ClearPendingAsync(Guid tenderId)`

Implementation notes:
- Centralize reminder generation logic here.
- Enforce all business rules here, not in controllers.

---

## Background Processing (Reminder Sender)

### TenderReminderWorker : BackgroundService
Loop every 60 seconds:
1. Query pending reminders:
   - `SentAtUtc IS NULL`
   - `ReminderAtUtc <= UtcNow`
   - Join Tender + Owner
   - Tender not terminal
2. For each reminder:
   - Send email
   - Mark `SentAtUtc = UtcNow`
   - If failure: increment AttemptCount, set LastError (truncate), do not set SentAtUtc
3. Retry policy:
   - Max attempts: 5
   - Backoff: handled implicitly by next loop (optional: push ReminderAtUtc + 10 min on fail)

Concurrency:
- Use a DB transaction per reminder update (required).
- Single-instance deployment assumed for V1; if multiple instances are introduced, add a claim/lock to avoid double sends.

---

## Email Sending
### IEmailSender
- `Task SendAsync(string to, string subject, string htmlBody)`

Config (appsettings):
- SMTP host/port/user/pass OR SendGrid API key
- From address + display name
- Base URL for deep links

Email template (simple HTML):
- Tender Name
- Closing UTC datetime
- Owner name
- Status
- Link to tender details

---

## Persistence
- EF Core DbContext includes:
  - Identity tables
  - Tender, TenderReminder

Migrations:
- `dotnet ef migrations add Initial`
- `dotnet ef database update`

---

## Folder Structure (suggested)
- `Controllers/`
- `Views/`
- `Models/` (entities + enums)
- `Data/` (DbContext, configurations)
- `Services/`
  - `TenderService.cs`
  - `ReminderScheduler.cs`
  - `EmailSender.cs`
- `Workers/`
  - `TenderReminderWorker.cs`
- `ViewModels/`
- `Dtos/`
- `Infrastructure/` (time conversion helpers, constants)

---

## Coding Standards
- DRY: no duplicate reminder rule code across layers.
- Strong typing: use `DateTimeOffset`, enums, and validated DTOs/VMs.
- No logic in Views; format-only.
- Controllers: orchestrate only, no domain rules.
- Use `AsNoTracking()` for read-only queries.
- Ensure everything is `async` with cancellation tokens where sensible.

---

## Acceptance Criteria (Definition of Done)
- User can log in and create a tender with owner + closing datetime.
- Dashboard shows the tender in correct section based on closing time/status.
- Updating closing datetime regenerates reminders (no duplicates).
- Changing status to Submitted/Won/Lost/Cancelled prevents further reminders.
- Reminder emails send at the correct times (within 60s worker tick).
- Authorization rules enforced (non-owner cannot edit unless Admin).
- Basic validation prevents garbage data.

---

## Implementation Checklist (Build Order)
1. Create MVC project + Identity - done
2. Add EF Core + migrations
3. Implement entities + configurations + indexes/constraints - done
4. Implement TenderService (CRUD + dashboard queries) - done
5. Implement ReminderScheduler (generate/regenerate/clear) - done
6. Implement TenderReminderWorker + EmailSender
7. Implement Razor pages (Dashboard + Tender CRUD)
8. Add role seeding + initial Admin seed
9. Smoke test reminders with short intervals in dev config
10. Hardening: terminal status rule enforcement + uniqueness + idempotency

---

## Dev Config Overrides
For local testing, allow reminder offsets override:
- `ReminderOffsetsMinutes: [60, 30, 10, 2]`
Production uses the real schedule.

(No UI for this; config-only.)
