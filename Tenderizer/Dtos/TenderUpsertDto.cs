using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class TenderUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? Client { get; set; }
    public TenderCategory? Category { get; set; }
    public DateTimeOffset ClosingAtUtc { get; set; }
    public TenderStatus Status { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
}
