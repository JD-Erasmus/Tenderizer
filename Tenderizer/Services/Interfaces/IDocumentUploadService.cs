using Tenderizer.Dtos;

namespace Tenderizer.Services.Interfaces;

public interface IDocumentUploadService
{
    Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, bool isAdmin, CancellationToken cancellationToken = default);
}
