using Tenderizer.Dtos;
using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces;

public interface IDocumentUploadRoute
{
    DocumentType DocumentType { get; }
    Type MetadataType { get; }
    bool FileRequired { get; }
    bool MetadataRequired { get; }
    Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, object metadata, bool isAdmin, CancellationToken cancellationToken = default);
}

public interface IDocumentUploadRoute<TMetadata> : IDocumentUploadRoute
    where TMetadata : class
{
    Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, TMetadata metadata, bool isAdmin, CancellationToken cancellationToken = default);
}
