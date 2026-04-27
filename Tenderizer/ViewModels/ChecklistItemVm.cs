namespace Tenderizer.ViewModels;

public sealed class ChecklistItemVm
{
    public int Id { get; set; }
    public Guid TenderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool IsCompleted { get; set; }
    public string? LockedByUserId { get; set; }
    public DateTimeOffset? LockedAtUtc { get; set; }
    public DateTimeOffset? LockExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
