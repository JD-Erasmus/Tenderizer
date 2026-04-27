using System.ComponentModel.DataAnnotations;

namespace Tenderizer.Models;

public sealed class LibraryDocument
{
    public Guid Id { get; set; }

    public LibraryDocumentType Type { get; set; } = LibraryDocumentType.Other;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<LibraryDocumentVersion> Versions { get; set; } = new List<LibraryDocumentVersion>();
}
