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

DTOs and view models
--------------------

The MVC layer does not bind EF entities directly.

- `TenderUpsertDto` is the service input shape for create and update operations
- `TenderListItemVm`, `TenderDetailsVm`, and `TenderUpsertVm` are the main Razor view models

The dashboard also uses a view-local helper type, `Views/Home/TenderSectionVm.cs`, to group already-loaded list items into UI sections.
