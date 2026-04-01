using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Tenderizer.ViewModels;

public sealed class LibraryDocumentsIndexVm
{
    public IReadOnlyList<LibraryDocumentListItemVm> Items { get; set; } = Array.Empty<LibraryDocumentListItemVm>();
}

public sealed class LibraryDocumentCreateVm
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Initial file")]
    public IFormFile? File { get; set; }

    [Display(Name = "Expiry (UTC)")]
    public DateTimeOffset? ExpiryDateUtc { get; set; }
}

public sealed class LibraryDocumentVersionCreateVm
{
    [Required]
    [Display(Name = "New version file")]
    public IFormFile? File { get; set; }

    [Display(Name = "Expiry (UTC)")]
    public DateTimeOffset? ExpiryDateUtc { get; set; }
}

public sealed class LibraryDocumentListItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LibraryDocumentVersionSummaryVm? CurrentVersion { get; set; }
    public IReadOnlyList<LibraryDocumentVersionSummaryVm> Versions { get; set; } = Array.Empty<LibraryDocumentVersionSummaryVm>();
}

public sealed class LibraryDocumentDetailVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LibraryDocumentVersionSummaryVm? CurrentVersion { get; set; }
    public IReadOnlyList<LibraryDocumentVersionSummaryVm> Versions { get; set; } = Array.Empty<LibraryDocumentVersionSummaryVm>();
    public LibraryDocumentVersionCreateVm AddVersion { get; set; } = new();
}

public sealed class LibraryDocumentVersionSummaryVm
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ExpiryDateUtc { get; set; }
    public string ExpiryStatus { get; set; } = "No expiry";
}

public sealed class LibraryDocumentOptionVm
{
    public Guid VersionId { get; set; }
    public string Label { get; set; } = string.Empty;
}
