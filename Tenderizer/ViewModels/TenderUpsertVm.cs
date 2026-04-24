using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public TenderCategory? Category { get; set; }

    [Required]
    [Display(Name = "Closing (UTC)")]
    public DateTimeOffset ClosingAtUtc { get; set; }

    [Required]
    public TenderStatus Status { get; set; } = TenderStatus.Draft;

    [Display(Name = "Owner")]
    public string? OwnerUserId { get; set; }

    [Display(Name = "Assigned users")]
    public List<string> AssignedUserIds { get; set; } = [];

    public IReadOnlyList<SelectListItem> AssignedUserOptions { get; set; } = Array.Empty<SelectListItem>();

    [Display(Name = "Tender / RFP document")]
    public IFormFile? TenderRequestDocument { get; set; }
}
