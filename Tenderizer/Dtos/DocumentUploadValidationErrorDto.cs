namespace Tenderizer.Dtos;

public sealed class DocumentUploadValidationErrorDto
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
