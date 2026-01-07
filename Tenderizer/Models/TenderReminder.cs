using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public class TenderReminder
{
    public Guid Id { get; set; }

    [Required]
    public Guid TenderId { get; set; }

    public Tender Tender { get; set; } = default!;

    [Required]
    public DateTimeOffset ReminderAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public int AttemptCount { get; set; }

    [MaxLength(500)]
    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
