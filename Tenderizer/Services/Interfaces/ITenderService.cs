using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Interfaces;

public interface ITenderService
{
    Task<IReadOnlyList<TenderListItemVm>> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenderListItemVm>> GetListAsync(CancellationToken cancellationToken = default);
    Task<TenderDetailsVm> GetDetailsAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(TenderUpsertDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, TenderUpsertDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, TenderStatus status, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
