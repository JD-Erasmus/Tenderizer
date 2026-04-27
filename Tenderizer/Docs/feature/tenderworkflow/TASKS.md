Tender workflow implementation tasks
=====================================

This checklist lists the implementation tasks required to deliver the "Tender workflow" feature described in `Docs/TenderWorkflow.md`.

Priority order
--------------
1. Data model and migrations
2. Service interfaces and implementations
3. Notification plumbing
4. Tender document integration and checklist linkage
5. UI and controllers
6. Tests and migrations
7. Docs and deployment

Tasks
-----
Repository constraints
----------------------
- Do NOT generate, update, or edit EF Core migration files in this repository. Migration files or SQL must be created and reviewed by the repository maintainers.
- Always reference the contents of the `Docs` folder (notably `Docs/TenderWorkflow.md`, `Docs/Architecture.md`, and `Docs/Domain-Model.md`) when implementing features.

- Data model
  - [x] Add `ChecklistItem` entity with fields: `Id`, `TenderId`, `Title`, `Description`, `Required`, `IsCompleted`, `UploadedTenderDocumentId`, `LockedByUserId`, `LockedAtUtc`, `LockExpiresAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`.
  - [x] Add `TenderAssignment` join entity with `TenderId`, `UserId`, `AssignedAt`.
  - [x] Add `ChecklistGeneratedAt` (nullable) to `Tender` and update `TenderStatus` enum if needed.
  - [x] Configure EF relationships and indexes (index `ChecklistItem.TenderId`, `IsCompleted`, `LockedByUserId`).
  - [ ] Create EF Core migration artifacts manually and apply them via the normal team process (note: Copilot must not create or modify migration files).

- Configuration / templates
  - [x] Add `ChecklistTemplates` section to `appsettings.json` and bind to POCOs.
  - [x] Implement a `ChecklistTemplateProvider` that returns the `Default` template.

- Services
  - [x] Add `IChecklistService` interface: `GenerateChecklistAsync(Guid tenderId, string? templateName)`, `GetChecklistAsync(Guid tenderId)`, `MarkCompletedAsync(int checklistItemId, Guid? tenderDocumentId, string userId)`, `AddItemAsync(Guid tenderId, CreateChecklistItemDto, string userId)`, `UpdateItemAsync(...)`, `RemoveItemAsync(...)`.
  - [x] Implement `ChecklistService` for checklist generation, editing, and document-linked completion handling.
  - [x] Update `ITenderService`/`TenderService` to call `ChecklistService.GenerateChecklistAsync` when transitioning `Draft` -> `Identified` and to manage `ChecklistGeneratedAt`.
  - [x] Update `TenderDocumentService` to call `ChecklistService.MarkCompletedAsync` to link uploads.
  - [x] Wire the checklist service into tender status transitions and document upload flows.

- Notifications
  - [x] Add `INotificationService` that wraps existing `IEmailSender` and provides templates for assignment and status-change emails.
  - [x] Integrate notification calls into `TenderService` (on assignment and on status change) and into assignment APIs.

- Authorization
  - [x] Enforce that only owners/admins can assign users and change restricted statuses.
  - [x] Enforce that assigned users can add/edit/remove checklist items during `Identified`/`InProgress`.
  - [x] Server-side validation for all actions (do not rely on client checks).

- API / Controller changes
  - [x] Add endpoints for checklist: list, add, edit, remove, mark completed.
  - [x] Update `TendersController` and/or `TenderDocumentsController` to support checklist-related actions and uploads.
  - [x] Add UI model updates and DTOs for checklist operations.

- UI
  - [x] Provide checklist-linked uploads on `TenderDocuments/Index.cshtml` as the primary checklist completion surface.
  - [x] Show tender summary and attached-documents snapshot on `Tenders/Details.cshtml` for quick context.
  - [x] Update `Tenders/Edit` and `Create` to allow assigning users (admin/owner only).
  - [x] Remove checklist upload lock/unlock interactions from UI and service flow.

- Tests
  - [x] Unit tests for `ChecklistService` including generation, add/edit/remove, and mark-completed flows.
  - [x] Integration tests for `TenderService` status transitions and notification calls.
  - [ ] UI/functional tests for checklist add/upload/complete flows (optional but recommended).

- Docs and housekeeping
  - [x] Update `Docs/Architecture.md`, `Docs/Domain-Model.md`, and `Docs/TenderWorkflow.md` (done).
  - [x] Add migration notes and deployment instructions.
  - [x] Update release notes and update any seed data if required.

Optional / future
-----------------
- [ ] Persist editable checklist templates (DB + admin UI) instead of config-only templates.
- [ ] Add notification preferences and in-app notifications.
- [ ] Add audit/history for checklist changes and uploads.
- [ ] Add reminders based on overdue checklist items.

Estimated rough effort
---------------------
- Small team, 2-4 sprints depending on scope (UI polish, tests, editable templates).


If you want, I can scaffold the EF models and the `IChecklistService` interface next. Which task should I start?