using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public Guid TenderId { get; set; }
    public Tender Tender { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool IsCompleted { get; set; }

    public Guid? UploadedTenderDocumentId { get; set; }

    // Locking
    public string? LockedByUserId { get; set; }
    public DateTimeOffset? LockedAtUtc { get; set; }
    public DateTimeOffset? LockExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
