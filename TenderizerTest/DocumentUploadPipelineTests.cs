using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Tenderizer.Data;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Interfaces;
using Tenderizer.Services.Options;

namespace TenderizerTest;

public sealed class DocumentUploadPipelineTests : IDisposable
{
    private readonly string _contentRoot;

    public DocumentUploadPipelineTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"tenderizer-upload-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_contentRoot);
    }

    [Fact]
    public async Task UploadAsync_WhenRouteMissing_ReturnsRouteResolutionFailure()
    {
        var service = new DocumentUploadService(
            new DocumentUploadRouter(Array.Empty<IDocumentUploadRoute>()),
            new DocumentUploadRequestValidator());

        var result = await service.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.TenderDocument,
            OwnerId = Guid.NewGuid(),
            UploadedByUserId = "owner",
            MetadataJson = "{}",
        }, isAdmin: false);

        Assert.False(result.Success);
        Assert.Equal("route_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task UploadAsync_WhenMetadataJsonInvalid_ReturnsValidationFailure()
    {
        var route = new StubRoute(DocumentType.TenderDocument, typeof(TenderDocumentUploadMetadata), fileRequired: false, metadataRequired: true);
        var service = new DocumentUploadService(
            new DocumentUploadRouter([route]),
            new DocumentUploadRequestValidator());

        var result = await service.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.TenderDocument,
            OwnerId = Guid.NewGuid(),
            UploadedByUserId = "owner",
            MetadataJson = "{invalid-json}",
        }, isAdmin: false);

        Assert.False(result.Success);
        Assert.Equal("invalid_metadata", result.ErrorCode);
    }

    [Fact]
    public void BindMetadata_WhenPayloadValid_DeserializesToExpectedRouteMetadata()
    {
        var validator = new DocumentUploadRequestValidator();
        var route = new StubRoute(DocumentType.LibraryDocument, typeof(LibraryDocumentUploadMetadata), fileRequired: false, metadataRequired: true);

        var request = new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.LibraryDocument,
            OwnerId = Guid.NewGuid(),
            UploadedByUserId = "owner",
            MetadataJson = JsonSerializer.Serialize(new LibraryDocumentUploadMetadata
            {
                Name = "CV - Candidate",
                Type = LibraryDocumentType.Cv,
            })
        };

        var binding = validator.BindMetadata(request, route);

        Assert.True(binding.Success);
        var typed = Assert.IsType<LibraryDocumentUploadMetadata>(binding.Metadata);
        Assert.Equal("CV - Candidate", typed.Name);
        Assert.Equal(LibraryDocumentType.Cv, typed.Type);
    }

    [Fact]
    public async Task TenderRoute_WhenUploadingTenderDocument_MapsMetadataAndPersistsDocument()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, ownerUserId: "owner");
        var checklistItem = await CreateChecklistItemAsync(db, tender.Id, "Upload proposal");
        var store = CreateStore();

        var route = new TenderDocumentUploadRoute(new TenderDocumentService(db, store), db);

        var result = await route.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.TenderDocument,
            OwnerId = tender.Id,
            UploadedByUserId = "owner",
            File = CreateFile("proposal.pdf"),
        }, new TenderDocumentUploadMetadata
        {
            ChecklistItemId = checklistItem.Id,
            Category = TenderDocumentCategory.TechnicalProposal,
            DisplayName = "Technical Proposal",
        }, isAdmin: false);

        Assert.True(result.Success);

        var persistedTenderDocument = await db.TenderDocuments.AsNoTracking().SingleAsync(x => x.Id == result.DomainDocumentId);
        Assert.Equal(TenderDocumentCategory.TechnicalProposal, persistedTenderDocument.Category);
        Assert.Equal("Technical Proposal", persistedTenderDocument.DisplayName);

        var persistedChecklistItem = await db.ChecklistItems.AsNoTracking().SingleAsync(x => x.Id == checklistItem.Id);
        Assert.True(persistedChecklistItem.IsCompleted);

        var checklistEvidence = await db.ChecklistDocuments.AsNoTracking().SingleAsync(x => x.ChecklistItemId == checklistItem.Id);
        Assert.Equal(tender.Id, checklistEvidence.TenderId);
        Assert.Equal(persistedTenderDocument.StoredFileId, checklistEvidence.StoredFileId);
    }

    [Fact]
    public async Task LibraryRoute_WhenTypeIsCv_PersistsCvLibraryClassification()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var route = new LibraryDocumentUploadRoute(new LibraryDocumentService(db, CreateStore()), db);

        var result = await route.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.LibraryDocument,
            OwnerId = Guid.NewGuid(),
            UploadedByUserId = "admin",
            File = CreateFile("candidate-cv.docx"),
        }, new LibraryDocumentUploadMetadata
        {
            Name = "Candidate CV",
            Type = LibraryDocumentType.Cv,
        }, isAdmin: true);

        Assert.True(result.Success);

        var persisted = await db.LibraryDocuments.AsNoTracking().SingleAsync(x => x.Id == result.DomainDocumentId);
        Assert.Equal(LibraryDocumentType.Cv, persisted.Type);
    }

    [Fact]
    public async Task ChecklistRoute_WhenLinkingLibraryVersion_SupportsLibraryLinkedEvidence()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, ownerUserId: "owner");
        var checklistItem = await CreateChecklistItemAsync(db, tender.Id, "Attach tax evidence");

        var store = CreateStore();
        var libraryService = new LibraryDocumentService(db, store);
        var libraryDocumentId = await libraryService.CreateAsync(new LibraryDocumentCreateDto
        {
            Name = "Tax Certificate",
            Type = LibraryDocumentType.Certificate,
            File = CreateFile("tax.pdf"),
        }, "admin");

        var libraryVersion = await db.LibraryDocumentVersions.AsNoTracking().SingleAsync(x => x.LibraryDocumentId == libraryDocumentId);

        var route = new ChecklistEvidenceUploadRoute(db, store);

        var result = await route.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.ChecklistEvidence,
            OwnerId = tender.Id,
            UploadedByUserId = "owner",
            File = null,
        }, new ChecklistEvidenceUploadMetadata
        {
            ChecklistItemId = checklistItem.Id,
            LibraryDocumentVersionId = libraryVersion.Id,
            DisplayName = "Library Evidence",
        }, isAdmin: false);

        Assert.True(result.Success);

        var evidence = await db.ChecklistDocuments.AsNoTracking().SingleAsync(x => x.Id == result.DomainDocumentId);
        Assert.Equal(libraryVersion.Id, evidence.LibraryDocumentVersionId);
        Assert.Equal(libraryVersion.StoredFileId, evidence.StoredFileId);
    }

    [Fact]
    public async Task ChecklistRoute_WhenUploadingDirectEvidence_PersistsWithCorrectOwnership()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, ownerUserId: "owner");
        var checklistItem = await CreateChecklistItemAsync(db, tender.Id, "Upload compliance evidence");

        var route = new ChecklistEvidenceUploadRoute(db, CreateStore());

        var result = await route.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.ChecklistEvidence,
            OwnerId = tender.Id,
            UploadedByUserId = "owner",
            File = CreateFile("evidence.pdf"),
        }, new ChecklistEvidenceUploadMetadata
        {
            ChecklistItemId = checklistItem.Id,
            DisplayName = "Evidence Upload",
        }, isAdmin: false);

        Assert.True(result.Success);

        var evidence = await db.ChecklistDocuments.AsNoTracking().SingleAsync(x => x.Id == result.DomainDocumentId);
        Assert.Equal(tender.Id, evidence.TenderId);
        Assert.Equal(checklistItem.Id, evidence.ChecklistItemId);

        var item = await db.ChecklistItems.AsNoTracking().SingleAsync(x => x.Id == checklistItem.Id);
        Assert.True(item.IsCompleted);
    }

    [Fact]
    public async Task UploadService_WhenTenderUploadHasChecklistItem_CompletesChecklistAndPersistsTenderDocument()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, ownerUserId: "owner");
        var checklistItem = await CreateChecklistItemAsync(db, tender.Id, "Provide tender response");
        var store = CreateStore();

        var routes = new IDocumentUploadRoute[]
        {
            new TenderDocumentUploadRoute(new TenderDocumentService(db, store), db),
            new LibraryDocumentUploadRoute(new LibraryDocumentService(db, store), db),
            new ChecklistEvidenceUploadRoute(db, store),
        };

        var service = new DocumentUploadService(new DocumentUploadRouter(routes), new DocumentUploadRequestValidator());

        var result = await service.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.TenderDocument,
            OwnerId = tender.Id,
            UploadedByUserId = "owner",
            File = CreateFile("response.pdf"),
            MetadataJson = JsonSerializer.Serialize(new TenderDocumentUploadMetadata
            {
                ChecklistItemId = checklistItem.Id,
                Category = TenderDocumentCategory.TenderRequestDocument,
                DisplayName = "Tender Response",
            }),
        }, isAdmin: false);

        Assert.True(result.Success);
        Assert.NotNull(result.DomainDocumentId);

        var document = await db.TenderDocuments.AsNoTracking().SingleAsync(x => x.Id == result.DomainDocumentId);
        Assert.Equal(TenderDocumentCategory.TenderRequestDocument, document.Category);

        var evidence = await db.ChecklistDocuments.AsNoTracking().SingleAsync(x => x.ChecklistItemId == checklistItem.Id);
        Assert.Equal(document.StoredFileId, evidence.StoredFileId);
    }

    [Fact]
    public async Task ChecklistRoute_WhenUserUnauthorized_BlocksUploadAndDoesNotPersistEvidence()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var tender = await CreateTenderAsync(db, ownerUserId: "owner");
        var checklistItem = await CreateChecklistItemAsync(db, tender.Id, "Unauthorized upload attempt");

        var route = new ChecklistEvidenceUploadRoute(db, CreateStore());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => route.UploadAsync(new DocumentUploadRequestDto
        {
            DocumentType = DocumentType.ChecklistEvidence,
            OwnerId = tender.Id,
            UploadedByUserId = "outsider",
            File = CreateFile("unauthorized.pdf"),
        }, new ChecklistEvidenceUploadMetadata
        {
            ChecklistItemId = checklistItem.Id,
            DisplayName = "Blocked",
        }, isAdmin: false));

        Assert.False(await db.ChecklistDocuments.AsNoTracking().AnyAsync(x => x.ChecklistItemId == checklistItem.Id));
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    private PrivateFileStore CreateStore()
    {
        var environment = new FakeWebHostEnvironment
        {
            ContentRootPath = _contentRoot,
            WebRootPath = Path.Combine(_contentRoot, "wwwroot"),
        };

        var options = Options.Create(new DocumentStorageOptions
        {
            PrivateRootFolder = "App_Data/Documents",
            MaxFileSizeMb = 5,
            AllowedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"],
        });

        return new PrivateFileStore(environment, options);
    }

    private static IFormFile CreateFile(string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(fileName);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static async Task<Tender> CreateTenderAsync(ApplicationDbContext db, string ownerUserId)
    {
        var tender = new Tender
        {
            Id = Guid.NewGuid(),
            Name = "Tender Upload Test",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(10),
            Status = TenderStatus.InProgress,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.Tenders.Add(tender);
        await db.SaveChangesAsync();
        return tender;
    }

    private static async Task<ChecklistItem> CreateChecklistItemAsync(ApplicationDbContext db, Guid tenderId, string title)
    {
        var item = new ChecklistItem
        {
            TenderId = tenderId,
            Title = title,
            Required = true,
            IsCompleted = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private sealed class StubRoute : IDocumentUploadRoute
    {
        public StubRoute(DocumentType documentType, Type metadataType, bool fileRequired, bool metadataRequired)
        {
            DocumentType = documentType;
            MetadataType = metadataType;
            FileRequired = fileRequired;
            MetadataRequired = metadataRequired;
        }

        public DocumentType DocumentType { get; }
        public Type MetadataType { get; }
        public bool FileRequired { get; }
        public bool MetadataRequired { get; }

        public Task<DocumentUploadResultDto> UploadAsync(DocumentUploadRequestDto request, object metadata, bool isAdmin, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DocumentUploadResultDto
            {
                Success = true,
                DomainDocumentId = Guid.NewGuid(),
            });
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TenderizerTest";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
