using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public sealed class TenderDocumentCvMetadata
{
    public Guid TenderDocumentId { get; set; }

    [MaxLength(200)]
    public string? PersonName { get; set; }

    [MaxLength(200)]
    public string? ProjectRole { get; set; }

    public bool IsLeadConsultant { get; set; }

    public TenderDocument TenderDocument { get; set; } = null!;
}
