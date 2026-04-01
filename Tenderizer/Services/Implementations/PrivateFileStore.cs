using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Tenderizer.Models;
using Tenderizer.Services.Interfaces;
using Tenderizer.Services.Options;

namespace Tenderizer.Services.Implementations;

public sealed class PrivateFileStore : IPrivateFileStore
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    private readonly IWebHostEnvironment _environment;
    private readonly DocumentStorageOptions _options;

    public PrivateFileStore(IWebHostEnvironment environment, IOptions<DocumentStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<StoredFileWriteResult> SaveNewAsync(
        IFormFile file,
        Guid storedFileId,
        string? relativeFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Select a non-empty file.");
        }

        ValidateFile(file);

        var root = ResolvePrivateRoot();
        var folder = NormalizeRelativePath(relativeFolder);
        var targetDirectory = ResolvePath(root, folder);
        Directory.CreateDirectory(targetDirectory);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{storedFileId:N}_{SanitizeFileNameWithoutExtension(file.FileName)}{extension}";
        var relativePath = string.IsNullOrEmpty(folder)
            ? storedFileName
            : $"{folder}/{storedFileName}";
        var physicalPath = ResolvePath(root, relativePath);

        string sha256;
        await using (var sourceStream = file.OpenReadStream())
        await using (var targetStream = File.Create(physicalPath))
        using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, bytesRead);
                await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            sha256 = Convert.ToHexString(hash.GetHashAndReset());
        }

        return new StoredFileWriteResult
        {
            RelativePath = relativePath,
            StoredFileName = storedFileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = ResolveContentType(file.FileName, file.Headers?.ContentType.ToString()),
            LengthBytes = file.Length,
            Sha256 = sha256,
        };
    }

    public Task<StoredFileReadResult> OpenReadAsync(
        StoredFile storedFile,
        string? downloadFileName = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(storedFile);

        var physicalPath = ResolvePath(ResolvePrivateRoot(), storedFile.RelativePath);
        if (!File.Exists(physicalPath))
        {
            throw new FileNotFoundException("Document file was not found on disk.", physicalPath);
        }

        return Task.FromResult(new StoredFileReadResult
        {
            Stream = File.OpenRead(physicalPath),
            ContentType = ResolveContentType(storedFile.OriginalFileName, storedFile.ContentType),
            DownloadFileName = string.IsNullOrWhiteSpace(downloadFileName)
                ? storedFile.OriginalFileName
                : downloadFileName,
        });
    }

    private string ResolvePrivateRoot()
    {
        var configured = string.IsNullOrWhiteSpace(_options.PrivateRootFolder)
            ? "App_Data/Documents"
            : _options.PrivateRootFolder;

        var root = Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configured));

        var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "wwwroot"))
            : Path.GetFullPath(_environment.WebRootPath);

        var normalizedWebRoot = EnsureTrailingSeparator(webRoot);
        if (root.StartsWith(normalizedWebRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Document storage root must be outside wwwroot.");
        }

        Directory.CreateDirectory(root);
        return root;
    }

    private void ValidateFile(IFormFile file)
    {
        var maxBytes = _options.MaxFileSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException($"Files larger than {_options.MaxFileSizeMb} MB are not allowed.");
        }

        var extension = Path.GetExtension(file.FileName);
        var allowedExtensions = _options.AllowedExtensions
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.StartsWith('.') ? value : $".{value}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (allowedExtensions.Count == 0)
        {
            return;
        }

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Files with the '{extension}' extension are not allowed.");
        }
    }

    private static string ResolvePath(string root, string storedPath)
    {
        var fullRoot = Path.GetFullPath(root);
        var combined = string.IsNullOrEmpty(storedPath)
            ? fullRoot
            : Path.GetFullPath(Path.Combine(fullRoot, storedPath.Replace('/', Path.DirectorySeparatorChar)));

        EnsureWithinRoot(fullRoot, combined);
        return combined;
    }

    private static void EnsureWithinRoot(string root, string candidate)
    {
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        var normalizedCandidate = Path.GetFullPath(candidate);

        if (string.Equals(
            normalizedCandidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The supplied path is outside the configured storage root.");
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static string NormalizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var segments = value
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            return string.Empty;
        }

        if (segments.Any(static segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Relative paths cannot contain '.' or '..' segments.");
        }

        return string.Join('/', segments);
    }

    private static string SanitizeFileNameWithoutExtension(string originalFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return "document";
        }

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(baseName
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray())
            .Trim();

        return string.IsNullOrWhiteSpace(sanitized)
            ? "document"
            : sanitized;
    }

    private static string ResolveContentType(string fileName, string? reportedContentType)
    {
        if (!string.IsNullOrWhiteSpace(reportedContentType))
        {
            return reportedContentType;
        }

        return ContentTypeProvider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}
