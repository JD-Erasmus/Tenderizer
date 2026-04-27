namespace Tenderizer.Models;

public sealed class LibraryDocumentVersion
{
    public Guid Id { get; set; }
    public Guid LibraryDocumentId { get; set; }
    public Guid StoredFileId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset? ExpiryDateUtc { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public LibraryDocument LibraryDocument { get; set; } = null!;
    public StoredFile StoredFile { get; set; } = null!;
    public ICollection<TenderDocument> TenderDocuments { get; set; } = new List<TenderDocument>();
    public ICollection<ChecklistDocument> ChecklistDocuments { get; set; } = new List<ChecklistDocument>();
}
