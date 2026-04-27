using Microsoft.AspNetCore.Http;
using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class DocumentUploadRequestDto
{
    public DocumentType DocumentType { get; set; }
    public Guid OwnerId { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
    public string? MetadataJson { get; set; }
}
