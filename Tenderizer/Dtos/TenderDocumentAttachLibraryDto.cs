using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class TenderDocumentAttachLibraryDto
{
    public Guid LibraryDocumentVersionId { get; set; }
    public TenderDocumentCategory Category { get; set; }
    public string? DisplayName { get; set; }
    public string? PersonName { get; set; }
    public string? ProjectRole { get; set; }
    public bool IsLeadConsultant { get; set; }
}
