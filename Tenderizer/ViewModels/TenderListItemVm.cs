using Tenderizer.Models;

namespace Tenderizer.ViewModels;

public sealed class TenderListItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Client { get; set; }
    public DateTimeOffset ClosingAtUtc { get; set; }
    public TenderStatus Status { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
}
