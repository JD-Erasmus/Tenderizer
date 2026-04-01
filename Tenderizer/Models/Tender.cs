using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public class Tender
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(200)]
    public string? Client { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [Required]
    public DateTimeOffset ClosingAtUtc { get; set; }

    [Required]
    public TenderStatus Status { get; set; }

    [Required]
    public string OwnerUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<TenderReminder> Reminders { get; set; } = new List<TenderReminder>();
    public ICollection<TenderDocument> Documents { get; set; } = new List<TenderDocument>();
}
