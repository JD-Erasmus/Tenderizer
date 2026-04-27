Domain Model
============

This document summarizes the current domain entities, status model, and business rules implemented in Tenderizer.

Entities
--------

Tender

- `Id`: `Guid` primary key
- `Name`: required, max 200 chars
- `ReferenceNumber`: optional, max 100 chars
- `Client`: optional, max 200 chars
- `Category`: optional, max 100 chars
- `ClosingAtUtc`: required closing timestamp stored in UTC
- `Status`: required `TenderStatus`
- `OwnerUserId`: required Identity user id
- `CreatedAtUtc` and `UpdatedAtUtc`: audit timestamps
- `Reminders`: collection of `TenderReminder`

Service-layer rules for `Tender`:

- `Name`, `ReferenceNumber`, `Client`, and `Category` are trimmed before save
- Non-admin users always create tenders for themselves
- Admin users can assign or reassign `OwnerUserId`
- Non-admin users cannot create or update non-terminal tenders with a closing time in the past

TenderReminder

- `Id`: `Guid` primary key
- `TenderId`: foreign key to `Tender`
- `ReminderAtUtc`: when the email should be sent
- `SentAtUtc`: set after a successful send
- `AttemptCount`: number of worker attempts
- `LastError`: last failure message, max 500 chars
- `CreatedAtUtc`: insert timestamp

Persistence rules for `TenderReminder`:

- There is a unique index on `(TenderId, ReminderAtUtc)`
- Pending reminders are rows where `SentAtUtc` is `null`
- Deleting a tender cascades to its reminders

TenderStatus

The enum values are:

- `Draft`
- `Identified`
- `InProgress`
- `Submitted`
- `Won`
- `Lost`
- `Cancelled`

Status groups used by the application:

- Reminder-active statuses: `Draft`, `Identified`, `InProgress`
- Terminal statuses: `Submitted`, `Won`, `Lost`, `Cancelled`

Authorization rules
-------------------

- Any authenticated user can view the dashboard, tender list, and tender details
- Any authenticated user can create a tender
- Only the owner or an `Admin` can edit a tender
- Only an `Admin` can delete a tender

Relationships and indexes
-------------------------

`ApplicationDbContext` configures:

- `DbSet<Tender>` and `DbSet<TenderReminder>`
- A one-to-many relationship from `Tender` to `TenderReminder`
- Indexes on `Tender.ClosingAtUtc`, `Tender.Status`, and `Tender.OwnerUserId`
- Indexes on reminder state and schedule fields for worker lookups

Checklist completion linkage
----------------------------

- Checklist evidence is modeled explicitly as `ChecklistDocument`.
- `ChecklistDocument` is tender-owned evidence and references `Tender`, `ChecklistItem`, `StoredFile`, and optional `LibraryDocumentVersion`.
- `ChecklistItem` completion is updated when valid evidence is attached.
- Authorization for uploads and checklist changes is enforced server-side based on owner/admin/assignment rules.

Document classification and ownership
-------------------------------------

- `LibraryDocument` is the reusable identity aggregate and includes `Type` classification (`Cv`, `Certificate`, `Policy`, `Template`, `Other`).
- CV is modeled as library classification (`LibraryDocument.Type = Cv`), not as tender-specific metadata.
- `TenderDocument` remains tender-owned submission context.
- `ChecklistDocument` remains tender-owned evidence context, even when linked to a reusable library version.

Reminder lifecycle
------------------

When a tender is created or updated:

1. `TenderService` saves the tender.
2. The service asks `ReminderScheduler` to clear or regenerate pending reminders.
3. Reminder generation stops immediately for terminal statuses.
4. For reminder-active statuses, offsets are applied relative to `ClosingAtUtc`.
5. Only future reminder timestamps are kept.

When the worker processes reminders:

- Success sets `SentAtUtc` and clears `LastError`
- Failure increments `AttemptCount`, stores `LastError`, and reschedules the reminder for 10 minutes later
- Attempts stop after 5 failures

Notifications and assignment events
----------------------------------

- Assigning a user to a tender triggers an email notification to the assigned user.
- Changing a tender's status triggers email notifications to the tender owner and assigned users for key transitions (`Draft` -> `Identified`, `Identified` -> `InProgress`, `InProgress` -> `Submitted`).
- Notifications should be sent asynchronously and use the existing `IEmailSender` implementation behind a `INotificationService` orchestration layer.

Checklist edit permissions
-------------------------

- Any user assigned to a tender may add, edit, or remove checklist items while the tender is in `Identified` or `InProgress` statuses. Edits should be audited (who, when) if audit requirements exist.
- Owners and admins may always edit or remove checklist items regardless of assignment.

DTOs and view models
--------------------

The MVC layer does not bind EF entities directly.

- `TenderUpsertDto` is the service input shape for create and update operations
- `TenderListItemVm`, `TenderDetailsVm`, and `TenderUpsertVm` are the main Razor view models

The dashboard also uses a view-local helper type, `Views/Home/TenderSectionVm.cs`, to group already-loaded list items into UI sections.
