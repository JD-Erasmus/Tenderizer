using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class LibraryDocumentUploadMetadata
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LibraryDocumentType Type { get; set; } = LibraryDocumentType.Other;
    public DateTimeOffset? ExpiryDateUtc { get; set; }
}
