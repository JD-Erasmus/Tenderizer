using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class ChecklistEvidenceUploadRoute : IDocumentUploadRoute<ChecklistEvidenceUploadMetadata>
{
    private readonly ApplicationDbContext _db;
    private readonly IPrivateFileStore _privateFileStore;

    public ChecklistEvidenceUploadRoute(ApplicationDbContext db, IPrivateFileStore privateFileStore)
    {
        _db = db;
        _privateFileStore = privateFileStore;
    }

    public DocumentType DocumentType => DocumentType.ChecklistEvidence;
    public Type MetadataType => typeof(ChecklistEvidenceUploadMetadata);
    public bool FileRequired => false;
    public bool MetadataRequired => true;

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, object metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (metadata is not ChecklistEvidenceUploadMetadata typedMetadata)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Checklist evidence metadata payload is invalid.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.MetadataJson),
                        Message = "Checklist evidence metadata payload is invalid.",
                    }
                ]
            };
        }

        return await UploadAsync(request, typedMetadata, isAdmin, cancellationToken);
    }

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, ChecklistEvidenceUploadMetadata metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var tender = await _db.Tenders
            .Include(x => x.Assignments)
            .SingleOrDefaultAsync(x => x.Id == request.OwnerId, cancellationToken);

        if (tender is null)
        {
            return NotFound("owner", "Tender owner context was not found.");
        }

        if (!CanManageTender(tender, request.UploadedByUserId, isAdmin))
        {
            throw new UnauthorizedAccessException("Only the owner, an assigned user, or an admin can manage checklist evidence.");
        }

        var checklistItem = await _db.ChecklistItems
            .SingleOrDefaultAsync(x => x.Id == metadata.ChecklistItemId, cancellationToken);

        if (checklistItem is null)
        {
            return NotFound(nameof(metadata.ChecklistItemId), "Checklist item was not found.");
        }

        if (checklistItem.TenderId != request.OwnerId)
        {
            return Invalid(nameof(metadata.ChecklistItemId), "Checklist item does not belong to the specified tender.");
        }

        if (metadata.LibraryDocumentVersionId.HasValue && request.File is not null)
        {
            return Invalid(nameof(request.File), "Do not provide a file when linking evidence from a library document version.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        Guid storedFileId;
        string? storedPath;
        Guid? libraryDocumentVersionId = null;

        if (metadata.LibraryDocumentVersionId.HasValue)
        {
            var libraryVersion = await _db.LibraryDocumentVersions
                .Include(x => x.LibraryDocument)
                .Include(x => x.StoredFile)
                .SingleOrDefaultAsync(x => x.Id == metadata.LibraryDocumentVersionId.Value, cancellationToken);

            if (libraryVersion is null)
            {
                return NotFound(nameof(metadata.LibraryDocumentVersionId), "Library document version was not found.");
            }

            storedFileId = libraryVersion.StoredFileId;
            storedPath = libraryVersion.StoredFile.RelativePath;
            libraryDocumentVersionId = libraryVersion.Id;
        }
        else
        {
            if (request.File is null)
            {
                return Invalid(nameof(request.File), "File is required when checklist evidence is uploaded directly.");
            }

            storedFileId = Guid.NewGuid();
            var writeResult = await _privateFileStore.SaveNewAsync(
                request.File,
                storedFileId,
                $"tenders/{request.OwnerId:D}/checklist/{metadata.ChecklistItemId}",
                cancellationToken);

            storedPath = writeResult.RelativePath;

            _db.StoredFiles.Add(new StoredFile
            {
                Id = storedFileId,
                StorageProvider = "FileSystem",
                RelativePath = writeResult.RelativePath,
                StoredFileName = writeResult.StoredFileName,
                OriginalFileName = writeResult.OriginalFileName,
                ContentType = writeResult.ContentType,
                LengthBytes = writeResult.LengthBytes,
                Sha256 = writeResult.Sha256,
                UploadedByUserId = request.UploadedByUserId,
                UploadedAtUtc = utcNow,
            });
        }

        var checklistDocument = new ChecklistDocument
        {
            Id = Guid.NewGuid(),
            TenderId = request.OwnerId,
            ChecklistItemId = metadata.ChecklistItemId,
            StoredFileId = storedFileId,
            LibraryDocumentVersionId = libraryDocumentVersionId,
            DisplayName = ResolveDisplayName(metadata.DisplayName, request.File?.FileName),
            UploadedByUserId = request.UploadedByUserId,
            UploadedAtUtc = utcNow,
        };

        checklistItem.IsCompleted = true;
        checklistItem.UpdatedAtUtc = utcNow;

        _db.ChecklistDocuments.Add(checklistDocument);
        await _db.SaveChangesAsync(cancellationToken);

        return new DocumentUploadResultDto
        {
            Success = true,
            DomainDocumentId = checklistDocument.Id,
            StoredFileId = checklistDocument.StoredFileId,
            StoredPath = storedPath,
        };
    }

    private static bool CanManageTender(Tender tender, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return true;
        }

        if (string.Equals(tender.OwnerUserId, userId, StringComparison.Ordinal))
        {
            return true;
        }

        return (tender.Status is TenderStatus.Identified or TenderStatus.InProgress) &&
               tender.Assignments.Any(x => string.Equals(x.UserId, userId, StringComparison.Ordinal));
    }

    private static DocumentUploadResultDto NotFound(string field, string message)
    {
        return new DocumentUploadResultDto
        {
            Success = false,
            ErrorCode = "not_found",
            ErrorMessage = message,
            ValidationErrors =
            [
                new DocumentUploadValidationErrorDto
                {
                    Field = field,
                    Message = message,
                }
            ]
        };
    }

    private static DocumentUploadResultDto Invalid(string field, string message)
    {
        return new DocumentUploadResultDto
        {
            Success = false,
            ErrorCode = "validation_failed",
            ErrorMessage = message,
            ValidationErrors =
            [
                new DocumentUploadValidationErrorDto
                {
                    Field = field,
                    Message = message,
                }
            ]
        };
    }

    private static string ResolveDisplayName(string? requestedDisplayName, string? fallbackFileName)
    {
        var trimmedDisplayName = requestedDisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedDisplayName))
        {
            return trimmedDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(fallbackFileName))
        {
            return Path.GetFileName(fallbackFileName);
        }

        return "Checklist Evidence";
    }
}
