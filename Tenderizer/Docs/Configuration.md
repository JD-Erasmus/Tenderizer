Configuration
=============

Tenderizer uses standard ASP.NET Core configuration sources:

- `appsettings.json`
- `appsettings.{Environment}.json`
- environment variables
- user secrets

Important sections
------------------

Connection string

- `ConnectionStrings:DefaultConnection`
- Used by `ApplicationDbContext`
- Must point to a SQL Server database for the web app

Email

Bound to `EmailOptions` and used by both reminder emails and Identity confirmation emails.

- `Email:FromAddress`
- `Email:FromDisplayName`
- `Email:BaseUrl`
- `Email:SmtpHost`
- `Email:SmtpPort`
- `Email:SmtpEnableSsl`
- `Email:SmtpUser`
- `Email:SmtpPass`

Notes:

- `BaseUrl` exists in the options class but is not currently used by the reminder email templates
- If SMTP settings are missing, reminder delivery and confirmation-email resend will fail at runtime

Identity seed

Bound to `IdentitySeedOptions`.

- `IdentitySeed:AdminEmail`
- `IdentitySeed:AdminPassword`

On startup, `IdentitySeeder`:

- Creates the `Admin` and `User` roles if they do not exist
- Creates the configured admin user if no admin currently exists
- Otherwise promotes the first existing user to `Admin` if no admin exists

Reminder offsets

- `ReminderOffsetsMinutes`
- Optional integer array of minute offsets before closing

Development defaults
--------------------

`appsettings.Development.json` currently seeds:

- `admin@local.test`
- `ChangeMe!12345`

That makes local first-run access predictable as long as the app runs in the `Development` environment.
