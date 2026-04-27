using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class TenderDocumentUploadMetadata
{
    public int? ChecklistItemId { get; set; }
    public TenderDocumentCategory Category { get; set; }
    public string? DisplayName { get; set; }
}
