namespace Tenderizer.Models;

public class TenderAssignment
{
    public Guid TenderId { get; set; }
    public Tender Tender { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset AssignedAt { get; set; }
}
