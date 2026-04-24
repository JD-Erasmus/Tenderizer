using Microsoft.AspNetCore.Http;
using Tenderizer.Models;

namespace Tenderizer.Dtos;

public sealed class TenderDocumentUploadDto
{
    public int? ChecklistItemId { get; set; }

    public TenderDocumentCategory Category { get; set; }
    public string? DisplayName { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? PersonName { get; set; }
    public string? ProjectRole { get; set; }
    public bool IsLeadConsultant { get; set; }
}
