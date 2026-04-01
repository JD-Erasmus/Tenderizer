using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public sealed class TenderDocument
{
    public Guid Id { get; set; }
    public Guid TenderId { get; set; }
    public Guid StoredFileId { get; set; }
    public Guid? LibraryDocumentVersionId { get; set; }
    public TenderDocumentCategory Category { get; set; }
    public TenderDocumentSourceType SourceType { get; set; }

    [Required]
    [MaxLength(260)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string AttachedByUserId { get; set; } = string.Empty;

    public DateTimeOffset AttachedAtUtc { get; set; }

    public Tender Tender { get; set; } = null!;
    public StoredFile StoredFile { get; set; } = null!;
    public LibraryDocumentVersion? LibraryDocumentVersion { get; set; }
    public TenderDocumentCvMetadata? CvMetadata { get; set; }
}
