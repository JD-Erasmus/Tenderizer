using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Tenderizer.Dtos;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Interfaces;
using Tenderizer.Services.Options;

namespace TenderizerTest;

public sealed class TenderDocumentServiceTests : IDisposable
{
    private readonly string _contentRoot;

    public TenderDocumentServiceTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"tenderizer-tender-doc-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_contentRoot);
    }

    [Fact]
    public async Task AttachLibraryVersionAsync_PinsSpecificStoredFileAndVersion()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var store = CreateStore();
        var libraryService = new LibraryDocumentService(db, store);
        var tenderDocumentService = new TenderDocumentService(db, new NoOpChecklistService(), store);

        db.Tenders.Add(new Tender
        {
            Id = Guid.NewGuid(),
            Name = "Windhoek Proposal",
            OwnerUserId = "owner",
            ClosingAtUtc = DateTimeOffset.UtcNow.AddDays(14),
            Status = TenderStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var documentId = await libraryService.CreateAsync(new LibraryDocumentCreateDto
        {
            Name = "Tax Certificate",
            File = CreateFile("tax-v1.docx"),
        }, "admin");

        var version = db.LibraryDocumentVersions.Single(x => x.LibraryDocumentId == documentId);
        var tenderId = db.Tenders.Select(x => x.Id).Single();

        var tenderDocumentId = await tenderDocumentService.AttachLibraryVersionAsync(tenderId, new TenderDocumentAttachLibraryDto
        {
            LibraryDocumentVersionId = version.Id,
            Category = TenderDocumentCategory.Certificate,
            DisplayName = "Pinned Tax Certificate",
        }, "owner", isAdmin: false);

        var attached = db.TenderDocuments.Single(x => x.Id == tenderDocumentId);
        Assert.Equal(version.Id, attached.LibraryDocumentVersionId);
        Assert.Equal(version.StoredFileId, attached.StoredFileId);
        Assert.Equal(TenderDocumentSourceType.LibraryDocumentVersion, attached.SourceType);
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
            AllowedExtensions = [".pdf", ".doc", ".docx"],
        });

        return new PrivateFileStore(environment, options);
    }

    private static IFormFile CreateFile(string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(fileName);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "File", fileName);
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

    private sealed class NoOpChecklistService : IChecklistService
    {
        public Task GenerateChecklistAsync(Guid tenderId, string? templateName = null) => Task.CompletedTask;
        public Task<IEnumerable<ChecklistItem>> GetChecklistAsync(Guid tenderId, string userId) => Task.FromResult<IEnumerable<ChecklistItem>>(Array.Empty<ChecklistItem>());
        public Task MarkCompletedAsync(int checklistItemId, Guid? tenderDocumentId, string userId) => Task.CompletedTask;
        public Task<ChecklistItem> AddItemAsync(Guid tenderId, Tenderizer.Dtos.CreateChecklistItemDto dto, string userId) => throw new NotSupportedException();
        public Task UpdateItemAsync(int checklistItemId, Tenderizer.Dtos.UpdateChecklistItemDto dto, string userId) => Task.CompletedTask;
        public Task RemoveItemAsync(int checklistItemId, string userId) => Task.CompletedTask;
    }
}
