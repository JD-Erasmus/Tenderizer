Tenderizer
==========

Tenderizer is an internal ASP.NET Core 8 MVC application for tracking tenders, ownership, closing dates, and reminder emails. It stores tenders in SQL Server, uses ASP.NET Identity for authentication, and runs an in-process background worker that sends reminder emails ahead of closing time.

Overview
--------

- Authenticated users can view the dashboard, tender list, and tender details.
- Any authenticated user can create tenders.
- Only the owner of a tender or an `Admin` can edit it.
- Only an `Admin` can delete it.
- Reminder emails are generated for active tenders and sent by a hosted background worker.

Technology stack
----------------

- .NET 8
- ASP.NET Core MVC and Razor Pages
- Entity Framework Core with SQL Server
- ASP.NET Core Identity with `Admin` and `User` roles
- Bootstrap 5, jQuery, and jquery-validation
- xUnit with SQLite-backed tests for core service logic

Getting started
---------------

Prerequisites:

- .NET SDK 8 or later
- SQL Server or LocalDB
- SMTP settings if you want reminder emails or Identity confirmation emails to send successfully

From the repository root:

```bash
dotnet restore
dotnet build
dotnet ef database update --project Tenderizer
dotnet run --project Tenderizer
```

Development defaults:

- `Tenderizer/appsettings.json` contains the LocalDB connection string.
- `Tenderizer/appsettings.Development.json` seeds a development admin account:
  - Email: `admin@local.test`
  - Password: `ChangeMe!12345`
- Launch profiles live in `Tenderizer/Properties/launchSettings.json`.

Current auth model:

- The app is login-only. There is no self-registration page or user administration UI in this repo.
- Additional users must be provisioned outside the current UI, for example through direct Identity administration or custom tooling.

Configuration
-------------

Important settings:

- `ConnectionStrings:DefaultConnection`: SQL Server connection for `ApplicationDbContext`
- `Email`: SMTP settings used for tender reminders and Identity confirmation emails
- `IdentitySeed`: optional startup seed for the initial admin user
- `ReminderOffsetsMinutes`: optional minute offsets for reminder generation

If `ReminderOffsetsMinutes` is not supplied, the scheduler defaults to reminders at 7 days, 3 days, and 24 hours before closing.

Background processing
---------------------

The reminder system has two parts:

- `ReminderScheduler` regenerates pending reminders whenever a tender changes.
- `TenderReminderWorker` polls every 60 seconds for due reminders and sends email.

Failure handling:

- Failed reminders are retried up to 5 times.
- Each retry is rescheduled to 10 minutes in the future.
- The last error message is stored on the reminder row.

The current worker design assumes a single running app instance. It does not use a distributed lease, so multiple app instances can race and send duplicate reminders.

Running tests
-------------

Run the full test suite from the repository root:

```bash
dotnet test
```

Documentation
-------------

Implementation-focused docs live under `Tenderizer/Docs`:

- `Architecture.md`
- `Domain-Model.md`
- `Background-Processing.md`
- `Security-and-Identity.md`
- `Configuration.md`
- `Development-Guide.md`
- `Testing Strategy.md`
- `UI Instructions.md`
- `V1 Agent Instructions.md`
