using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class LibraryDocumentUploadRoute : IDocumentUploadRoute<LibraryDocumentUploadMetadata>
{
    private readonly ILibraryDocumentService _libraryDocumentService;
    private readonly ApplicationDbContext _db;

    public LibraryDocumentUploadRoute(ILibraryDocumentService libraryDocumentService, ApplicationDbContext db)
    {
        _libraryDocumentService = libraryDocumentService;
        _db = db;
    }

    public DocumentType DocumentType => DocumentType.LibraryDocument;
    public Type MetadataType => typeof(LibraryDocumentUploadMetadata);
    public bool FileRequired => true;
    public bool MetadataRequired => true;

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, object metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (metadata is not LibraryDocumentUploadMetadata typedMetadata)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Library document metadata payload is invalid.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.MetadataJson),
                        Message = "Library document metadata payload is invalid.",
                    }
                ]
            };
        }

        return await UploadAsync(request, typedMetadata, isAdmin, cancellationToken);
    }

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, LibraryDocumentUploadMetadata metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (request.File is null)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "missing_file",
                ErrorMessage = "File is required for library uploads.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.File),
                        Message = "File is required for library uploads.",
                    }
                ]
            };
        }

        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Library document name is required.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(metadata.Name),
                        Message = "Library document name is required.",
                    }
                ]
            };
        }

        var documentId = await _libraryDocumentService.CreateAsync(new LibraryDocumentCreateDto
        {
            Name = metadata.Name,
            Description = metadata.Description,
            Type = metadata.Type,
            File = request.File,
            ExpiryDateUtc = metadata.ExpiryDateUtc,
        }, request.UploadedByUserId, cancellationToken);

        var persistedVersion = await _db.LibraryDocumentVersions
            .AsNoTracking()
            .Include(x => x.StoredFile)
            .Where(x => x.LibraryDocumentId == documentId)
            .OrderByDescending(x => x.VersionNumber)
            .FirstAsync(cancellationToken);

        return new DocumentUploadResultDto
        {
            Success = true,
            DomainDocumentId = documentId,
            StoredFileId = persistedVersion.StoredFileId,
            StoredPath = persistedVersion.StoredFile.RelativePath,
        };
    }
}
