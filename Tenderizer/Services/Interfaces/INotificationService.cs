using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces;

public interface INotificationService
{
    Task NotifyTenderAssignedAsync(Guid tenderId, string tenderName, string assignedUserId, CancellationToken cancellationToken = default);
    Task NotifyTenderStatusChangedAsync(Guid tenderId, string tenderName, TenderStatus previousStatus, TenderStatus currentStatus, CancellationToken cancellationToken = default);
}
