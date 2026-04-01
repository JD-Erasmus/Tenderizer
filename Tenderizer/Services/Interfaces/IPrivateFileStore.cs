using Microsoft.AspNetCore.Http;
using Tenderizer.Models;

namespace Tenderizer.Services.Interfaces;

public interface IPrivateFileStore
{
    Task<StoredFileWriteResult> SaveNewAsync(
        IFormFile file,
        Guid storedFileId,
        string? relativeFolder,
        CancellationToken cancellationToken = default);

    Task<StoredFileReadResult> OpenReadAsync(
        StoredFile storedFile,
        string? downloadFileName = null,
        CancellationToken cancellationToken = default);
}
