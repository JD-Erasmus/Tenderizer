using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Interfaces;

public interface ILibraryDocumentService
{
    Task<IReadOnlyList<LibraryDocumentListItemVm>> GetListAsync(CancellationToken cancellationToken = default);
    Task<LibraryDocumentDetailVm> GetDetailsAsync(Guid libraryDocumentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LibraryDocumentOptionVm>> GetVersionOptionsAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(LibraryDocumentCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task AddVersionAsync(Guid libraryDocumentId, LibraryDocumentVersionCreateDto dto, string userId, CancellationToken cancellationToken = default);
    Task<DocumentDownloadDescriptor> GetDownloadAsync(Guid libraryDocumentId, Guid versionId, CancellationToken cancellationToken = default);
}
