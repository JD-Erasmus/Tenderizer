using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Implementations;

public sealed class TenderDocumentService : ITenderDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IPrivateFileStore _privateFileStore;

    public TenderDocumentService(ApplicationDbContext db, IPrivateFileStore privateFileStore)
    {
        _db = db;
        _privateFileStore = privateFileStore;
    }

    public async Task<TenderDocumentsIndexVm> GetIndexAsync(Guid tenderId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var tender = await _db.Tenders
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == tenderId, cancellationToken);

        if (tender is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        EnsureCanManageTender(tender, userId, isAdmin);

        var documents = await _db.TenderDocuments
            .AsNoTracking()
            .Where(x => x.TenderId == tenderId)
            .Include(x => x.StoredFile)
            .Include(x => x.LibraryDocumentVersion)
            .ThenInclude(x => x!.LibraryDocument)
            .Include(x => x.CvMetadata)
            .OrderBy(x => x.Category)
            .ThenByDescending(x => x.AttachedAtUtc)
            .ToListAsync(cancellationToken);

        var libraryVersions = await _db.LibraryDocumentVersions
            .AsNoTracking()
            .Include(x => x.LibraryDocument)
            .OrderBy(x => x.LibraryDocument.Name)
            .ThenByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        var libraryOptions = libraryVersions.Select(x => new LibraryDocumentOptionVm
        {
            VersionId = x.Id,
            Label = $"{x.LibraryDocument.Name} - v{x.VersionNumber} ({LibraryDocumentService.DescribeExpiry(x.ExpiryDateUtc)})",
        }).ToList();

        return new TenderDocumentsIndexVm
        {
            TenderId = tender.Id,
            TenderName = tender.Name,
            Documents = documents.Select(MapTenderDocument).ToList(),
            LibraryDocumentOptions = libraryOptions,
        };
    }

    public async Task<Guid> UploadAsync(Guid tenderId, TenderDocumentUploadDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var tender = await _db.Tenders.SingleOrDefaultAsync(x => x.Id == tenderId, cancellationToken);
        if (tender is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        EnsureCanManageTender(tender, userId, isAdmin);

        var utcNow = DateTimeOffset.UtcNow;
        var storedFileId = Guid.NewGuid();
        var fileResult = await _privateFileStore.SaveNewAsync(
            dto.File,
            storedFileId,
            $"tenders/{tenderId:D}/uploads",
            cancellationToken);

        var storedFile = CreateStoredFile(storedFileId, fileResult, userId, utcNow);
        var documentId = Guid.NewGuid();
        var document = new TenderDocument
        {
            Id = documentId,
            TenderId = tenderId,
            StoredFileId = storedFileId,
            Category = dto.Category,
            SourceType = TenderDocumentSourceType.Upload,
            DisplayName = ResolveDisplayName(dto.DisplayName, fileResult.OriginalFileName),
            AttachedByUserId = userId,
            AttachedAtUtc = utcNow,
        };

        ApplyCvMetadata(document, dto.Category, dto.PersonName, dto.ProjectRole, dto.IsLeadConsultant);

        _db.StoredFiles.Add(storedFile);
        _db.TenderDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return documentId;
    }

    public async Task<Guid> AttachLibraryVersionAsync(Guid tenderId, TenderDocumentAttachLibraryDto dto, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var tender = await _db.Tenders.SingleOrDefaultAsync(x => x.Id == tenderId, cancellationToken);
        if (tender is null)
        {
            throw new KeyNotFoundException("Tender not found.");
        }

        EnsureCanManageTender(tender, userId, isAdmin);

        var libraryVersion = await _db.LibraryDocumentVersions
            .AsNoTracking()
            .Include(x => x.StoredFile)
            .Include(x => x.LibraryDocument)
            .SingleOrDefaultAsync(x => x.Id == dto.LibraryDocumentVersionId, cancellationToken);

        if (libraryVersion is null)
        {
            throw new KeyNotFoundException("Library document version not found.");
        }

        var document = new TenderDocument
        {
            Id = Guid.NewGuid(),
            TenderId = tenderId,
            StoredFileId = libraryVersion.StoredFileId,
            LibraryDocumentVersionId = libraryVersion.Id,
            Category = dto.Category,
            SourceType = TenderDocumentSourceType.LibraryDocumentVersion,
            DisplayName = ResolveDisplayName(
                dto.DisplayName,
                $"{libraryVersion.LibraryDocument.Name} v{libraryVersion.VersionNumber}"),
            AttachedByUserId = userId,
            AttachedAtUtc = DateTimeOffset.UtcNow,
        };

        ApplyCvMetadata(document, dto.Category, dto.PersonName, dto.ProjectRole, dto.IsLeadConsultant);

        _db.TenderDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        return document.Id;
    }

    public async Task<DocumentDownloadDescriptor> GetDownloadAsync(Guid tenderId, Guid tenderDocumentId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var document = await _db.TenderDocuments
            .AsNoTracking()
            .Include(x => x.StoredFile)
            .Include(x => x.Tender)
            .SingleOrDefaultAsync(x => x.TenderId == tenderId && x.Id == tenderDocumentId, cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("Tender document not found.");
        }

        EnsureCanManageTender(document.Tender, userId, isAdmin);

        return new DocumentDownloadDescriptor
        {
            StoredFile = document.StoredFile,
            DownloadFileName = BuildDownloadFileName(document.DisplayName, document.StoredFile.OriginalFileName),
        };
    }

    private static void EnsureCanManageTender(Tender tender, string userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (!string.Equals(tender.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Only the owner or an admin can manage tender documents.");
        }
    }

    private static TenderDocumentListItemVm MapTenderDocument(TenderDocument document)
    {
        return new TenderDocumentListItemVm
        {
            Id = document.Id,
            DisplayName = document.DisplayName,
            FileName = document.StoredFile.OriginalFileName,
            Category = document.Category,
            SourceType = document.SourceType,
            LengthBytes = document.StoredFile.LengthBytes,
            AttachedAtUtc = document.AttachedAtUtc,
            LibraryDocumentName = document.LibraryDocumentVersion?.LibraryDocument.Name,
            LibraryVersionNumber = document.LibraryDocumentVersion?.VersionNumber,
            LibraryVersionExpiryDateUtc = document.LibraryDocumentVersion?.ExpiryDateUtc,
            LibraryVersionExpiryStatus = LibraryDocumentService.DescribeExpiry(document.LibraryDocumentVersion?.ExpiryDateUtc),
            PersonName = document.CvMetadata?.PersonName,
            ProjectRole = document.CvMetadata?.ProjectRole,
            IsLeadConsultant = document.CvMetadata?.IsLeadConsultant ?? false,
        };
    }

    private static StoredFile CreateStoredFile(
        Guid storedFileId,
        StoredFileWriteResult fileResult,
        string userId,
        DateTimeOffset uploadedAtUtc)
    {
        return new StoredFile
        {
            Id = storedFileId,
            StorageProvider = "FileSystem",
            RelativePath = fileResult.RelativePath,
            StoredFileName = fileResult.StoredFileName,
            OriginalFileName = fileResult.OriginalFileName,
            ContentType = fileResult.ContentType,
            LengthBytes = fileResult.LengthBytes,
            Sha256 = fileResult.Sha256,
            UploadedByUserId = userId,
            UploadedAtUtc = uploadedAtUtc,
        };
    }

    private static void ApplyCvMetadata(
        TenderDocument document,
        TenderDocumentCategory category,
        string? personName,
        string? projectRole,
        bool isLeadConsultant)
    {
        if (category != TenderDocumentCategory.Cv)
        {
            return;
        }

        document.CvMetadata = new TenderDocumentCvMetadata
        {
            TenderDocumentId = document.Id,
            PersonName = personName?.Trim(),
            ProjectRole = projectRole?.Trim(),
            IsLeadConsultant = isLeadConsultant,
        };
    }

    private static string ResolveDisplayName(string? requestedDisplayName, string fallback)
    {
        var displayName = requestedDisplayName?.Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? fallback
            : displayName;
    }

    private static string BuildDownloadFileName(string displayName, string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return originalFileName;
        }

        if (!string.IsNullOrWhiteSpace(Path.GetExtension(displayName)))
        {
            return displayName;
        }

        return $"{displayName}{Path.GetExtension(originalFileName)}";
    }
}
