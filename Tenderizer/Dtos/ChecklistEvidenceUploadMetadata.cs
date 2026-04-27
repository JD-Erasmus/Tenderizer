namespace Tenderizer.Dtos;

public sealed class ChecklistEvidenceUploadMetadata
{
    public int ChecklistItemId { get; set; }
    public string? DisplayName { get; set; }
    public Guid? LibraryDocumentVersionId { get; set; }
}
