using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tenderizer.Services.Interfaces;

public interface IUserLookupService
{
    Task<IReadOnlyList<SelectListItem>> GetUserSelectListAsync(CancellationToken cancellationToken = default);
}
