using System.ComponentModel.DataAnnotations;
using Tenderizer.Models;

namespace Tenderizer.ViewModels;

public sealed class TenderUpsertVm
{
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
    [Display(Name = "Closing (UTC)")]
    public DateTimeOffset ClosingAtUtc { get; set; }

    [Required]
    public TenderStatus Status { get; set; } = TenderStatus.Draft;

    [Display(Name = "Owner")]
    public string? OwnerUserId { get; set; }
}
