using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Tenderizer.Models;

namespace Tenderizer.ViewModels;

public sealed class TenderDocumentsIndexVm
{
    public Guid TenderId { get; set; }
    public string TenderName { get; set; } = string.Empty;
    public bool CanViewChecklist { get; set; }
    public IReadOnlyList<ChecklistItemVm> ChecklistItems { get; set; } = Array.Empty<ChecklistItemVm>();
    public IReadOnlyList<TenderDocumentListItemVm> Documents { get; set; } = Array.Empty<TenderDocumentListItemVm>();
    public IReadOnlyList<LibraryDocumentOptionVm> LibraryDocumentOptions { get; set; } = Array.Empty<LibraryDocumentOptionVm>();
    public TenderDocumentUploadVm Upload { get; set; } = new();
    public TenderDocumentAttachLibraryVm AttachLibrary { get; set; } = new();
}

public sealed class TenderDocumentUploadVm
{
    [Display(Name = "Checklist item")]
    public int? ChecklistItemId { get; set; }

    [Required]
    public TenderDocumentCategory Category { get; set; } = TenderDocumentCategory.Other;

    [MaxLength(260)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [Required]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    [MaxLength(200)]
    [Display(Name = "Person name")]
    public string? PersonName { get; set; }

    [MaxLength(200)]
    [Display(Name = "Role in project")]
    public string? ProjectRole { get; set; }

    [Display(Name = "Lead consultant")]
    public bool IsLeadConsultant { get; set; }
}

public sealed class TenderDocumentAttachLibraryVm
{
    [Required]
    [Display(Name = "Library version")]
    public Guid? LibraryDocumentVersionId { get; set; }

    [Required]
    public TenderDocumentCategory Category { get; set; } = TenderDocumentCategory.Certificate;

    [MaxLength(260)]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [MaxLength(200)]
    [Display(Name = "Person name")]
    public string? PersonName { get; set; }

    [MaxLength(200)]
    [Display(Name = "Role in project")]
    public string? ProjectRole { get; set; }

    [Display(Name = "Lead consultant")]
    public bool IsLeadConsultant { get; set; }
}

public sealed class TenderDocumentListItemVm
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public TenderDocumentCategory Category { get; set; }
    public TenderDocumentSourceType SourceType { get; set; }
    public long LengthBytes { get; set; }
    public DateTimeOffset AttachedAtUtc { get; set; }
    public string? LibraryDocumentName { get; set; }
    public int? LibraryVersionNumber { get; set; }
    public DateTimeOffset? LibraryVersionExpiryDateUtc { get; set; }
    public string LibraryVersionExpiryStatus { get; set; } = string.Empty;
    public string? PersonName { get; set; }
    public string? ProjectRole { get; set; }
    public bool IsLeadConsultant { get; set; }
}
