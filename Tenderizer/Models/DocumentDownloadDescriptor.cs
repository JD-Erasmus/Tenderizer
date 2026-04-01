namespace Tenderizer.Models;

public sealed class DocumentDownloadDescriptor
{
    public StoredFile StoredFile { get; init; } = null!;
    public string DownloadFileName { get; init; } = string.Empty;
}
