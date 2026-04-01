using Microsoft.AspNetCore.Http;

namespace Tenderizer.Dtos;

public sealed class LibraryDocumentCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IFormFile File { get; set; } = null!;
    public DateTimeOffset? ExpiryDateUtc { get; set; }
}
