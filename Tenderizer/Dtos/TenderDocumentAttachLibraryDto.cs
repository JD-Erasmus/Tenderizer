using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class TenderDocumentAttachLibraryDto
{
    public Guid LibraryDocumentVersionId { get; set; }
    public TenderDocumentCategory Category { get; set; }
    public string? DisplayName { get; set; }
}
