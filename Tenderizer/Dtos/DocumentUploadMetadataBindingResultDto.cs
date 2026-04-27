namespace Tenderizer.Dtos;

public sealed class DocumentUploadMetadataBindingResultDto
{
    public bool Success { get; set; }
    public object? Metadata { get; set; }
    public IReadOnlyList<DocumentUploadValidationErrorDto> ValidationErrors { get; set; } = [];
}
