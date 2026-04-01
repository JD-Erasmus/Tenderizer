using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public sealed class StoredFile
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StorageProvider { get; set; } = "FileSystem";

    [Required]
    [MaxLength(260)]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long LengthBytes { get; set; }

    [Required]
    [MaxLength(128)]
    public string Sha256 { get; set; } = string.Empty;

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTimeOffset UploadedAtUtc { get; set; }

    public ICollection<LibraryDocumentVersion> LibraryDocumentVersions { get; set; } = new List<LibraryDocumentVersion>();
    public ICollection<TenderDocument> TenderDocuments { get; set; } = new List<TenderDocument>();
}
