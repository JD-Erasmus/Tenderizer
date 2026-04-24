Development Guide
=================

This guide covers the current development workflow for the Tenderizer repository.

Repository structure
--------------------

- `Tenderizer.slnx`: solution entry point
- `Tenderizer/`: web application
- `TenderizerTest/`: xUnit test project

Useful commands
---------------

From the repository root:

```bash
dotnet restore
dotnet build
dotnet test
dotnet ef database update --project Tenderizer
dotnet run --project Tenderizer
```

Local development notes
-----------------------

- The default environment in `launchSettings.json` is `Development`
- `appsettings.Development.json` seeds a usable local admin account
- The web app uses SQL Server or LocalDB, not SQLite
- Tests use SQLite in-memory databases through `TestDbFactory`

Where to make changes
---------------------

- Add or update entity mapping in `Data/ApplicationDbContext.cs`
- Add migrations under `Data/Migrations`
- Put tender business rules in `Services/Implementations/TenderService.cs`
- Put reminder scheduling rules in `Services/Implementations/ReminderScheduler.cs`
- Put reminder dispatch logic in `Workers/TenderReminderWorker.cs`
- Keep controllers thin and use DTOs and view models for MVC input and output
- Keep JavaScript minimal; when it is needed, place it in a feature-based or model-based file rather than inline in views

Current implementation constraints
----------------------------------

- Dashboard section grouping lives in the view layer, not the service layer
- Reminder processing is designed for a single running app instance
- There is no in-app user management workflow yet

Testing focus
-------------

Automated coverage is currently strongest around:

- tender ownership and audit behavior
- reminder generation
- reminder regeneration edge cases

There is little automated coverage around Razor views and Identity page wiring, so UI and auth-path changes should be sanity-checked manually.
