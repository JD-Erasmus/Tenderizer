Security and Identity
=====================

Tenderizer uses ASP.NET Core Identity with cookie authentication.

Identity setup
--------------

`Program.cs` configures:

- `AddDefaultIdentity<IdentityUser>()`
- `AddRoles<IdentityRole>()`
- `AddEntityFrameworkStores<ApplicationDbContext>()`
- `options.SignIn.RequireConfirmedAccount = true`

Identity data is stored in the same database as the tender tables.

Roles
-----

The app seeds two roles:

- `Admin`
- `User`

Authorization rules
-------------------

- Any authenticated user can access the dashboard
- Any authenticated user can access the tender list
- Any authenticated user can open tender details
- Any authenticated user can create a tender
- Only the owner or an `Admin` can edit a tender
- Only an `Admin` can delete a tender

User provisioning
-----------------

The current UI is login-only:

- There is a login page
- There is a logout flow
- There is a basic account-management page
- There is a resend-confirmation page

What is not present:

- No public self-registration page
- No admin UI for managing users or roles

In practice, that means additional users must be provisioned outside the current MVC surface unless more Identity pages or admin tooling are added.

Email confirmation
------------------

Confirmed accounts are required for sign-in.

- The development admin seeded by `IdentitySeeder` is created with `EmailConfirmed = true`
- The resend-confirmation page uses the same SMTP settings as reminder emails through `IdentityEmailSenderAdapter`

If SMTP is not configured, confirmation resends will fail even though the page is present.
