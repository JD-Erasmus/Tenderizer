Architecture
============

This document describes the current implementation architecture of Tenderizer: projects, layers, runtime entry points, and the main request and reminder flows.

Solution layout
---------------

The solution contains two projects:

- `Tenderizer`: ASP.NET Core MVC app, Identity UI, EF Core data access, and the reminder worker
- `TenderizerTest`: xUnit tests for `TenderService` and `ReminderScheduler`

Important folders in `Tenderizer`:

- `Controllers`: MVC entry points for the dashboard and tender CRUD
- `Areas/Identity`: scaffolded Identity pages used for login, logout, account management, and email confirmation
- `Data`: `ApplicationDbContext` and EF Core migrations
- `Dtos`: controller-to-service request types
- `Models`: EF entities and the `TenderStatus` enum
- `Services/Interfaces`: application and infrastructure contracts
- `Services/Implementations`: service implementations, SMTP sender, and identity seeding
- `Services/Options`: configuration binding objects
- `ViewModels`: Razor view models used by the tender pages
- `Views`: MVC views, shared partials, and the dashboard section helper
- `Workers`: hosted background services

Startup and dependency injection
--------------------------------

`Program.cs` configures the web host and DI container:

- SQL Server `ApplicationDbContext` using `ConnectionStrings:DefaultConnection`
- ASP.NET Core Identity with roles and EF Core stores
- MVC controllers with views plus Razor Pages for Identity
- `ITenderService`, `IReminderScheduler`, and `IUserLookupService`
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
- `Privacy`: static view
- `Error`: standard error page

`TendersController`

- `[Authorize]`
- `Index`: list page with in-memory search by name or client
- `Details`: details page for any authenticated user
- `Create`: create form and submit handler
- `Edit`: owner-or-admin edit flow
- `Delete` and `DeleteConfirmed`: admin-only delete flow

The default route is `{controller=Home}/{action=Index}/{id?}`. Identity pages are mapped with `app.MapRazorPages()`.

Layering and responsibilities
-----------------------------

Presentation layer:

- Controllers gather request data, invoke services, and choose views
- Razor views render HTML and minor presentation-only grouping logic
- The dashboard grouping into Urgent, This Week, Active, and History happens in `Views/Home/Index.cshtml`, not in the service layer

Application layer:

- `TenderService` owns tender CRUD rules, audit fields, ownership checks for edits, and reminder regeneration triggers
- `UserLookupService` provides owner dropdown data for admins

Infrastructure layer:

- `ReminderScheduler` manages reminder rows in the database
- `SmtpEmailSender` sends HTML email through SMTP
- `IdentityEmailSenderAdapter` lets the Identity resend-confirmation page reuse the same SMTP pipeline
- `IdentitySeeder` ensures the `Admin` and `User` roles exist and that at least one admin is available
- `TenderReminderWorker` polls for due reminders and sends them

Data layer:

- `ApplicationDbContext` stores both Identity tables and the domain tables
- `Tender` and `TenderReminder` are mapped with indexes and a unique reminder-time constraint

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
3. `TenderService` calls `IReminderScheduler.RegenerateAsync()` or `ClearPendingAsync()` depending on the new status.

Reminder flow:

1. `ReminderScheduler` computes future reminder timestamps from the closing time and configured offsets.
2. Pending reminders are stored in `TenderReminders`.
3. `TenderReminderWorker` wakes every 60 seconds and queries for due reminders.
4. The worker loads the owner email from Identity and sends the message through `IEmailSender`.
5. The worker marks the reminder sent or reschedules it after a failure.

Operational note
----------------

Reminder dispatch currently assumes a single running web instance. There is no distributed lock or lease around due-reminder processing, so horizontal scaling can produce duplicate sends.
