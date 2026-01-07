using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tenderizer.Services.Options;

namespace Tenderizer.Services.Implementations;

public sealed class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IdentitySeedOptions _options;

    private static readonly string[] Roles = ["Admin", "User"];

    public IdentitySeeder(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        IOptions<IdentitySeedOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        // Ensure there is at least one Admin.
        var anyAdmin = await _userManager.GetUsersInRoleAsync("Admin");
        if (anyAdmin.Count > 0)
        {
            return;
        }

        IdentityUser? user = null;

        // Preferred: seed from configuration.
        if (!string.IsNullOrWhiteSpace(_options.AdminEmail) && !string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            user = await _userManager.FindByEmailAsync(_options.AdminEmail);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = _options.AdminEmail,
                    Email = _options.AdminEmail,
                    EmailConfirmed = true,
                };

                var create = await _userManager.CreateAsync(user, _options.AdminPassword);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", create.Errors.Select(e => e.Description))}");
                }
            }
        }
        else
        {
            // Fallback: promote the first existing user (created via UI).
            user = _userManager.Users.OrderBy(u => u.Email).FirstOrDefault();
        }

        if (user is null)
        {
            // No users exist yet; nothing to promote. Admin will be created when users are created.
            return;
        }

        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var addRole = await _userManager.AddToRoleAsync(user, "Admin");
            if (!addRole.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add user to Admin role: {string.Join(", ", addRole.Errors.Select(e => e.Description))}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, "User"))
        {
            var addUserRole = await _userManager.AddToRoleAsync(user, "User");
            if (!addUserRole.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add user to User role: {string.Join(", ", addUserRole.Errors.Select(e => e.Description))}");
            }
        }
    }
}
