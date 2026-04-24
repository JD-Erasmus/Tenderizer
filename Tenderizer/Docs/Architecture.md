Architecture
============

This document describes the current implementation architecture of Tenderizer: projects, layers, runtime entry points, and the main request and reminder flows.

Solution layout
---------------

The solution contains two projects:

- `Tenderizer`: ASP.NET Core MVC app, Identity UI, EF Core data access, private document storage, and the reminder worker
- `TenderizerTest`: xUnit tests for `TenderService`, `ReminderScheduler`, private file storage, library document versioning, and tender document attachment rules

Important folders in `Tenderizer`:

- `Controllers`: MVC entry points for the dashboard and tender CRUD
- `Areas/Identity`: scaffolded Identity pages used for login, logout, account management, and email confirmation
- `Data`: `ApplicationDbContext` and EF Core migrations
- `Dtos`: controller-to-service request types
- `Models`: EF entities for tenders, reminders, stored files, reusable library documents, versions, and tender attachments
- `Services/Interfaces`: application and infrastructure contracts
- `Services/Implementations`: service implementations, private file storage, SMTP sender, and identity seeding
- `Services/Options`: configuration binding objects
- `ViewModels`: Razor view models used by the tender pages and document pages
- `Views`: MVC views, shared partials, and the dashboard section helper
- `Workers`: hosted background services

Startup and dependency injection
--------------------------------

`Program.cs` configures the web host and DI container:

- SQL Server `ApplicationDbContext` using `ConnectionStrings:DefaultConnection`
- ASP.NET Core Identity with roles and EF Core stores
- MVC controllers with views plus Razor Pages for Identity
- `ITenderService`, `ITenderDocumentService`, `ILibraryDocumentService`, `IReminderScheduler`, and `IUserLookupService`
- `IPrivateFileStore` bound to the file-system-backed private document implementation
- `Tenderizer.Services.Interfaces.IEmailSender` for tender reminder delivery
- `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender` via `IdentityEmailSenderAdapter` for Identity confirmation emails
- `IdentitySeeder` for startup role and admin seeding
- `TenderReminderWorker` as a hosted service

After the host is built, startup code creates a scope and runs `IdentitySeeder.SeedAsync()`.

HTTP entry points
-----------------

`HomeController`

- `[Authorize]`
- `Index`: loads a flat dashboard list from `ITenderService.GetDashboardAsync()`
- `Error`: standard error page

`TendersController`

- `[Authorize]`
- `Index`: list page with in-memory search by name or client
- `Details`: details page for any authenticated user
- `Create`: create form and submit handler, with optional Tender / RFP document upload
- `Edit`: owner-or-admin edit flow
- `Delete` and `DeleteConfirmed`: admin-only delete flow

`TenderDocumentsController`

- `[Authorize]`
- `Index`: tender-specific document management page
- `Upload`: add a new private file to the tender
- `AttachLibrary`: pin a specific reusable library document version to a tender
- `Download`: authenticated document download guarded by tender ownership or admin access

`LibraryDocumentsController`

- `[Authorize(Roles = "Admin")]`
- `Index`: reusable library document list
- `Create`: create a reusable document and upload its first immutable version
- `Details`: view version history and upload a new version
- `Download`: authenticated download of a specific library document version

The default route is `{controller=Home}/{action=Index}/{id?}`. Identity pages are mapped with `app.MapRazorPages()`.

Layering and responsibilities
-----------------------------

Presentation layer:

- Controllers gather request data, invoke services, and choose views
- Razor views render HTML and minor presentation-only grouping logic
- The dashboard grouping into Urgent, This Week, Active, and History happens in `Views/Home/Index.cshtml`, not in the service layer

Application layer:

- `TenderService` owns tender CRUD rules, audit fields, ownership checks for edits, and reminder regeneration triggers
- `TenderDocumentService` owns tender document uploads, library-version attachments, CV metadata handling, and tender document authorization
- `LibraryDocumentService` owns reusable document creation, immutable versioning, and current-version switching
- `UserLookupService` provides owner dropdown data for admins

Infrastructure layer:

- `PrivateFileStore` writes immutable files under a private root outside `wwwroot`
- `ReminderScheduler` manages reminder rows in the database
- `SmtpEmailSender` sends HTML email through SMTP
- `IdentityEmailSenderAdapter` lets the Identity resend-confirmation page reuse the same SMTP pipeline
- `IdentitySeeder` ensures the `Admin` and `User` roles exist and that at least one admin is available
- `TenderReminderWorker` polls for due reminders and sends them

Data layer:

- `ApplicationDbContext` stores both Identity tables and the domain tables
- `Tender` and `TenderReminder` are mapped with indexes and a unique reminder-time constraint
- `StoredFile` is the immutable file record for everything written to private storage
- `LibraryDocument` and `LibraryDocumentVersion` implement reusable document versioning with one current version per document
- `TenderDocument` stores both the pinned `StoredFileId` and, when relevant, the source `LibraryDocumentVersionId`
- `TenderDocumentCvMetadata` holds optional CV-specific metadata for tender attachments

Primary flows
-------------

Request flow:

1. An authenticated request reaches MVC through the standard middleware pipeline.
2. The controller validates route, query, and form data.
3. The controller calls a service or returns a Razor view directly.
4. Services query or update `ApplicationDbContext`.
5. The controller returns a strongly typed Razor view model.

Create or update flow:

1. `TendersController` maps `TenderUpsertVm` to `TenderUpsertDto`.
2. `TenderService` trims input, enforces ownership and closing-date rules, and saves the `Tender`.
3. On create, `TendersController` optionally uploads the Tender / RFP file through `ITenderDocumentService`.
4. `TenderService` calls `IReminderScheduler.RegenerateAsync()` or `ClearPendingAsync()` depending on the new status.

Document flow:

1. A user uploads a file or attaches a reusable library document version.
2. `PrivateFileStore` saves new uploads under the configured private root and returns immutable file metadata.
3. `StoredFile` is created once per upload and never mutated.
4. `LibraryDocumentService` creates or advances `LibraryDocumentVersion` records, marking exactly one version current.
5. `TenderDocumentService` creates `TenderDocument` rows that pin the exact `StoredFileId` used at attachment time.
6. Downloads are served only through authenticated controller endpoints.

Reminder flow:

1. `ReminderScheduler` computes future reminder timestamps from the closing time and configured offsets.
2. Pending reminders are stored in `TenderReminders`.
3. `TenderReminderWorker` wakes every 60 seconds and queries for due reminders.
4. The worker loads the owner email from Identity and sends the message through `IEmailSender`.
5. The worker marks the reminder sent or reschedules it after a failure.

Operational note
----------------

Tender workflow
---------------

A new "Tender workflow" feature coordinates tender status transitions, team assignments, checklist generation, and collaborative document uploads. See `Docs/TenderWorkflow.md` for full design and migration notes.

- Statuses introduced: `Draft`, `Identified`, `InProgress`, `Completed`. `Draft` is the initial state; `Identified` indicates assigned team members and triggers checklist generation; `InProgress` enables collaborative uploads against checklist items; `Completed` marks the checklist finished and the tender ready for final review.
- Assignments: tenders support assigning one or more users (many-to-many) who may upload documents and complete checklist items.
- Checklist: persisted `ChecklistItem` entities are generated when a tender is identified. Uploads can satisfy checklist items and are tracked on the checklist.

This feature touches the application layer (`TenderService`, `TenderDocumentService`), the data layer (new `ChecklistItem` and `TenderAssignment` entities and migrations), and the presentation layer (tender details, checklist partials, and upload flows).

Operational note
----------------

Reminder dispatch currently assumes a single running web instance. There is no distributed lock or lease around due-reminder processing, so horizontal scaling can produce duplicate sends.
