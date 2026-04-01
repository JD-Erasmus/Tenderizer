using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Tenderizer.Models;
using Tenderizer.Services.Implementations;
using Tenderizer.Services.Options;

namespace TenderizerTest;

public sealed class PrivateFileStoreTests : IDisposable
{
    private readonly string _contentRoot;

    public PrivateFileStoreTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), $"tenderizer-private-store-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_contentRoot);
    }

    [Fact]
    public async Task SaveNewAsync_WritesFileOutsideWebRoot()
    {
        var store = CreateStore();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("proposal"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "File", "Proposal.docx");

        var storedFileId = Guid.NewGuid();
        var result = await store.SaveNewAsync(file, storedFileId, "tenders/demo");

        var expectedPath = Path.Combine(_contentRoot, "App_Data", "Documents", "tenders", "demo", result.StoredFileName);
        Assert.True(File.Exists(expectedPath));
        Assert.StartsWith("tenders/demo/", result.RelativePath, StringComparison.Ordinal);
        Assert.Equal("Proposal.docx", result.OriginalFileName);
        Assert.False(expectedPath.StartsWith(Path.Combine(_contentRoot, "wwwroot"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveNewAsync_WhenFolderUsesPathTraversal_Throws()
    {
        var store = CreateStore();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("proposal"));
        IFormFile file = new FormFile(stream, 0, stream.Length, "File", "Proposal.docx");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SaveNewAsync(file, Guid.NewGuid(), "../outside"));
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
