using Microsoft.EntityFrameworkCore;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.ViewModels;

namespace Tenderizer.Services.Implementations;

public sealed class LibraryDocumentService : ILibraryDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IPrivateFileStore _privateFileStore;

    public LibraryDocumentService(ApplicationDbContext db, IPrivateFileStore privateFileStore)
    {
        _db = db;
        _privateFileStore = privateFileStore;
    }

    public async Task<IReadOnlyList<LibraryDocumentListItemVm>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _db.LibraryDocuments
            .AsNoTracking()
            .Include(x => x.Versions.OrderByDescending(v => v.VersionNumber))
            .ThenInclude(x => x.StoredFile)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return documents.Select(document =>
        {
            var versions = document.Versions
                .OrderByDescending(x => x.VersionNumber)
                .Select(MapVersion)
                .ToList();

            return MapDocument(document.Id, document.Name, document.Description, versions);
        }).ToList();
    }

    public async Task<LibraryDocumentDetailVm> GetDetailsAsync(Guid libraryDocumentId, CancellationToken cancellationToken = default)
    {
        var document = await _db.LibraryDocuments
            .AsNoTracking()
            .Include(x => x.Versions.OrderByDescending(v => v.VersionNumber))
            .ThenInclude(x => x.StoredFile)
            .SingleOrDefaultAsync(x => x.Id == libraryDocumentId, cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("Library document not found.");
        }

        var versions = document.Versions
            .OrderByDescending(x => x.VersionNumber)
            .Select(MapVersion)
            .ToList();

        return new LibraryDocumentDetailVm
        {
            Id = document.Id,
            Name = document.Name,
            Description = document.Description,
            CurrentVersion = versions.FirstOrDefault(x => x.IsCurrent),
            Versions = versions,
        };
    }

    public async Task<IReadOnlyList<LibraryDocumentOptionVm>> GetVersionOptionsAsync(CancellationToken cancellationToken = default)
    {
        var versions = await _db.LibraryDocumentVersions
            .AsNoTracking()
            .Include(x => x.LibraryDocument)
            .OrderBy(x => x.LibraryDocument.Name)
            .ThenByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(x => new LibraryDocumentOptionVm
        {
            VersionId = x.Id,
            Label = BuildLibraryOptionLabel(
                x.LibraryDocument.Name,
                x.VersionNumber,
                x.IsCurrent,
                x.ExpiryDateUtc),
        }).ToList();
    }

    public async Task<Guid> CreateAsync(LibraryDocumentCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Library document name is required.");
        }

        var exists = await _db.LibraryDocuments.AnyAsync(x => x.Name == name, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("A library document with this name already exists.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        var documentId = Guid.NewGuid();
        var storedFileId = Guid.NewGuid();
        var fileResult = await _privateFileStore.SaveNewAsync(
            dto.File,
            storedFileId,
            $"library/{documentId:D}",
            cancellationToken);

        var storedFile = CreateStoredFile(storedFileId, fileResult, userId, utcNow);
        var libraryDocument = new LibraryDocument
        {
            Id = documentId,
            Name = name,
            Description = dto.Description?.Trim(),
            Type = dto.Type,
            CreatedByUserId = userId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
        };

        var version = new LibraryDocumentVersion
        {
            Id = Guid.NewGuid(),
            LibraryDocumentId = documentId,
            StoredFileId = storedFileId,
            VersionNumber = 1,
            IsCurrent = true,
            ExpiryDateUtc = dto.ExpiryDateUtc,
            CreatedByUserId = userId,
            CreatedAtUtc = utcNow,
        };

        _db.StoredFiles.Add(storedFile);
        _db.LibraryDocuments.Add(libraryDocument);
        _db.LibraryDocumentVersions.Add(version);
        await _db.SaveChangesAsync(cancellationToken);

        return documentId;
    }

    public async Task AddVersionAsync(Guid libraryDocumentId, LibraryDocumentVersionCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var document = await _db.LibraryDocuments
            .Include(x => x.Versions)
            .SingleOrDefaultAsync(x => x.Id == libraryDocumentId, cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("Library document not found.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        var storedFileId = Guid.NewGuid();
        var fileResult = await _privateFileStore.SaveNewAsync(
            dto.File,
            storedFileId,
            $"library/{libraryDocumentId:D}",
            cancellationToken);

        foreach (var currentVersion in document.Versions.Where(x => x.IsCurrent))
        {
            currentVersion.IsCurrent = false;
        }

        var nextVersionNumber = document.Versions.Count == 0
            ? 1
            : document.Versions.Max(x => x.VersionNumber) + 1;

        var storedFile = CreateStoredFile(storedFileId, fileResult, userId, utcNow);
        var version = new LibraryDocumentVersion
        {
            Id = Guid.NewGuid(),
            LibraryDocumentId = libraryDocumentId,
            StoredFileId = storedFileId,
            VersionNumber = nextVersionNumber,
            IsCurrent = true,
            ExpiryDateUtc = dto.ExpiryDateUtc,
            CreatedByUserId = userId,
            CreatedAtUtc = utcNow,
        };

        document.UpdatedAtUtc = utcNow;
        _db.StoredFiles.Add(storedFile);
        _db.LibraryDocumentVersions.Add(version);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentDownloadDescriptor> GetDownloadAsync(Guid libraryDocumentId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _db.LibraryDocumentVersions
            .AsNoTracking()
            .Include(x => x.StoredFile)
            .Include(x => x.LibraryDocument)
            .SingleOrDefaultAsync(
                x => x.LibraryDocumentId == libraryDocumentId && x.Id == versionId,
                cancellationToken);

        if (version is null)
        {
            throw new KeyNotFoundException("Library document version not found.");
        }

        return new DocumentDownloadDescriptor
        {
            StoredFile = version.StoredFile,
            DownloadFileName = version.StoredFile.OriginalFileName,
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

    private static LibraryDocumentVersionSummaryVm MapVersion(LibraryDocumentVersion version)
    {
        return new LibraryDocumentVersionSummaryVm
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            FileName = version.StoredFile.OriginalFileName,
            IsCurrent = version.IsCurrent,
            CreatedAtUtc = version.CreatedAtUtc,
            ExpiryDateUtc = version.ExpiryDateUtc,
            ExpiryStatus = DescribeExpiry(version.ExpiryDateUtc),
        };
    }

    private static LibraryDocumentListItemVm MapDocument(
        Guid id,
        string name,
        string? description,
        IReadOnlyList<LibraryDocumentVersionSummaryVm> versions)
    {
        return new LibraryDocumentListItemVm
        {
            Id = id,
            Name = name,
            Description = description,
            CurrentVersion = versions.FirstOrDefault(x => x.IsCurrent),
            Versions = versions,
        };
    }

    private static string BuildLibraryOptionLabel(
        string name,
        int versionNumber,
        bool isCurrent,
        DateTimeOffset? expiryDateUtc)
    {
        var currentSegment = isCurrent ? "current" : "historical";
        var expirySegment = DescribeExpiry(expiryDateUtc);
        return $"{name} - v{versionNumber} ({currentSegment}, {expirySegment})";
    }

    internal static string DescribeExpiry(DateTimeOffset? expiryDateUtc)
    {
        if (!expiryDateUtc.HasValue)
        {
            return "No expiry";
        }

        var utcNow = DateTimeOffset.UtcNow;
        if (expiryDateUtc.Value < utcNow)
        {
            return $"Expired {expiryDateUtc.Value:yyyy-MM-dd}";
        }

        return $"Valid until {expiryDateUtc.Value:yyyy-MM-dd}";
    }
}
