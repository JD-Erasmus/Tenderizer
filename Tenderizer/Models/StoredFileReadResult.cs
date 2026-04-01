namespace Tenderizer.Models;

public sealed class StoredFileReadResult
{
    public Stream Stream { get; init; } = Stream.Null;
    public string ContentType { get; init; } = "application/octet-stream";
    public string DownloadFileName { get; init; } = string.Empty;
}
