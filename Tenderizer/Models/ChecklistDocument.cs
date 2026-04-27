using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public sealed class ChecklistDocument
{
    public Guid Id { get; set; }
    public Guid TenderId { get; set; }
    public int ChecklistItemId { get; set; }
    public Guid StoredFileId { get; set; }
    public Guid? LibraryDocumentVersionId { get; set; }

    [Required]
    [MaxLength(260)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTimeOffset UploadedAtUtc { get; set; }

    public Tender Tender { get; set; } = null!;
    public ChecklistItem ChecklistItem { get; set; } = null!;
    public StoredFile StoredFile { get; set; } = null!;
    public LibraryDocumentVersion? LibraryDocumentVersion { get; set; }
}
