using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Interfaces;

public interface ITenderDocumentService
{
    Task<TenderDocumentsIndexVm> GetIndexAsync(Guid tenderId, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Guid> UploadAsync(Guid tenderId, TenderDocumentUploadDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Guid> AttachLibraryVersionAsync(Guid tenderId, TenderDocumentAttachLibraryDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<DocumentDownloadDescriptor> GetDownloadAsync(Guid tenderId, Guid tenderDocumentId, string userId, bool isAdmin, CancellationToken cancellationToken = default);
}
