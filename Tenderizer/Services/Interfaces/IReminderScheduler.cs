namespace Tenderizer.Services.Interfaces;

public interface IReminderScheduler
{
    Task RegenerateAsync(Guid tenderId, CancellationToken cancellationToken = default);
    Task ClearPendingAsync(Guid tenderId, CancellationToken cancellationToken = default);
}
