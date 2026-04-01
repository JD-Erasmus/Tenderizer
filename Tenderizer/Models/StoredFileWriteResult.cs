namespace Tenderizer.Models;

public sealed class StoredFileWriteResult
{
    public string RelativePath { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/octet-stream";
    public long LengthBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
}
