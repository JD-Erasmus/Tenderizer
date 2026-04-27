namespace Tenderizer.Dtos;

public sealed class DocumentUploadResultDto
{
    public bool Success { get; set; }
    public Guid? StoredFileId { get; set; }
    public Guid? DomainDocumentId { get; set; }
    public string? StoredPath { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<DocumentUploadValidationErrorDto> ValidationErrors { get; set; } = [];
}
