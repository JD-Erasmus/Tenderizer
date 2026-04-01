using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Tenderizer.Dtos;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Options;

namespace TenderizerTest;

public sealed class LibraryDocumentServiceTests : IDisposable
{
    private readonly string _contentRoot;

    public LibraryDocumentServiceTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"tenderizer-library-doc-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_contentRoot);
    }

    [Fact]
    public async Task AddVersionAsync_MarksPreviousCurrentVersionAsNotCurrent()
    {
        var (db, connection) = await TestDbFactory.CreateSqliteDbContextAsync();
        await using var _ = db;
        await using var __ = connection;

        var service = new LibraryDocumentService(db, CreateStore());

        var documentId = await service.CreateAsync(new LibraryDocumentCreateDto
        {
            Name = "Tax Certificate",
            File = CreateFile("tax-v1.docx"),
            ExpiryDateUtc = DateTimeOffset.UtcNow.AddDays(30),
        }, "admin");

        await service.AddVersionAsync(documentId, new LibraryDocumentVersionCreateDto
        {
            File = CreateFile("tax-v2.docx"),
            ExpiryDateUtc = DateTimeOffset.UtcNow.AddDays(60),
        }, "admin");

        var versions = db.LibraryDocumentVersions
            .Where(x => x.LibraryDocumentId == documentId)
            .OrderBy(x => x.VersionNumber)
            .ToList();

        Assert.Equal(2, versions.Count);
        Assert.False(versions[0].IsCurrent);
        Assert.True(versions[1].IsCurrent);
        Assert.Equal(2, versions[1].VersionNumber);
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
}
