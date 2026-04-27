using Tenderizer.Models;

namespace Tenderizer.ViewModels;

public sealed class TenderDetailsVm
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
    public IReadOnlyList<string> AssignedUserIds { get; set; } = Array.Empty<string>();
    public bool CanViewDocuments { get; set; }
    public IReadOnlyList<TenderDocumentListItemVm> Documents { get; set; } = Array.Empty<TenderDocumentListItemVm>();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
