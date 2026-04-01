using Tenderizer.Models;

namespace Tenderizer.ViewModels;

public sealed class TenderListItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Client { get; set; }
    public string? Category { get; set; }
    public DateTimeOffset ClosingAtUtc { get; set; }
    public TenderStatus Status { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
