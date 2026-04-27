using Tenderizer.Dtos;

namespace Tenderizer.Services.Interfaces;

public interface IDocumentUploadRequestValidator
{
    IReadOnlyList<DocumentUploadValidationErrorDto> ValidateBaseline(DocumentUploadRequestDto request, IDocumentUploadRoute route);
    DocumentUploadMetadataBindingResultDto BindMetadata(DocumentUploadRequestDto request, IDocumentUploadRoute route);
}
