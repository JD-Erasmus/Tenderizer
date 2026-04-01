using Microsoft.AspNetCore.Http;

namespace Tenderizer.Dtos;

public sealed class LibraryDocumentVersionCreateDto
{
    public IFormFile File { get; set; } = null!;
    public DateTimeOffset? ExpiryDateUtc { get; set; }
}
