using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class UserLookupService : IUserLookupService
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserLookupService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<SelectListItem>> GetUserSelectListAsync(CancellationToken cancellationToken = default)
    {
        // Default Identity uses string keys and users are stored in the IdentityDbContext.
        // This keeps lookup logic out of controllers.
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        return users
            .Select(u => new SelectListItem(u.Email ?? u.Id, u.Id))
            .ToList();
    }
}
