using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;

namespace Tenderizer.Services.Implementations;

public sealed class TenderDocumentUploadRoute : IDocumentUploadRoute<TenderDocumentUploadMetadata>
{
    private readonly ITenderDocumentService _tenderDocumentService;
    private readonly ApplicationDbContext _db;

    public TenderDocumentUploadRoute(ITenderDocumentService tenderDocumentService, ApplicationDbContext db)
    {
        _tenderDocumentService = tenderDocumentService;
        _db = db;
    }

    public DocumentType DocumentType => DocumentType.TenderDocument;
    public Type MetadataType => typeof(TenderDocumentUploadMetadata);
    public bool FileRequired => true;
    public bool MetadataRequired => true;

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, object metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (metadata is not TenderDocumentUploadMetadata typedMetadata)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "invalid_metadata",
                ErrorMessage = "Tender document metadata payload is invalid.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.MetadataJson),
                        Message = "Tender document metadata payload is invalid.",
                    }
                ]
            };
        }

        return await UploadAsync(request, typedMetadata, isAdmin, cancellationToken);
    }

    public async Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, TenderDocumentUploadMetadata metadata, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (request.File is null)
        {
            return new DocumentUploadResultDto
            {
                Success = false,
                ErrorCode = "missing_file",
                ErrorMessage = "File is required for tender uploads.",
                ValidationErrors =
                [
                    new DocumentUploadValidationErrorDto
                    {
                        Field = nameof(request.File),
                        Message = "File is required for tender uploads.",
                    }
                ]
            };
        }

        ChecklistItem? checklistItem = null;
        if (metadata.ChecklistItemId.HasValue)
        {
            checklistItem = await _db.ChecklistItems
                .SingleOrDefaultAsync(x => x.Id == metadata.ChecklistItemId.Value, cancellationToken);

            if (checklistItem is null)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    ErrorCode = "validation_failed",
                    ErrorMessage = "Checklist item was not found.",
                    ValidationErrors =
                    [
                        new DocumentUploadValidationErrorDto
                        {
                            Field = nameof(metadata.ChecklistItemId),
                            Message = "Checklist item was not found.",
                        }
                    ]
                };
            }

            if (checklistItem.TenderId != request.OwnerId)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    ErrorCode = "validation_failed",
                    ErrorMessage = "Checklist item does not belong to this tender.",
                    ValidationErrors =
                    [
                        new DocumentUploadValidationErrorDto
                        {
                            Field = nameof(metadata.ChecklistItemId),
                            Message = "Checklist item does not belong to this tender.",
                        }
                    ]
                };
            }
        }

        var tenderDocumentId = await _tenderDocumentService.UploadAsync(request.OwnerId, new TenderDocumentUploadDto
        {
            Category = metadata.Category,
            DisplayName = metadata.DisplayName,
            File = request.File,
        }, request.UploadedByUserId, isAdmin, cancellationToken);

        var persistedDocument = await _db.TenderDocuments
            .AsNoTracking()
            .Include(x => x.StoredFile)
            .SingleAsync(x => x.Id == tenderDocumentId, cancellationToken);

        if (checklistItem is not null)
        {
            var utcNow = DateTimeOffset.UtcNow;

            _db.ChecklistDocuments.Add(new ChecklistDocument
            {
                Id = Guid.NewGuid(),
                TenderId = request.OwnerId,
                ChecklistItemId = checklistItem.Id,
                StoredFileId = persistedDocument.StoredFileId,
                LibraryDocumentVersionId = persistedDocument.LibraryDocumentVersionId,
                DisplayName = persistedDocument.DisplayName,
                UploadedByUserId = request.UploadedByUserId,
                UploadedAtUtc = utcNow,
            });

            checklistItem.IsCompleted = true;
            checklistItem.UpdatedAtUtc = utcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new DocumentUploadResultDto
        {
            Success = true,
            DomainDocumentId = tenderDocumentId,
            StoredFileId = persistedDocument.StoredFileId,
            StoredPath = persistedDocument.StoredFile.RelativePath,
        };
    }
}
