namespace Tenderizer.Services.Options;

public sealed class EmailOptions
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "Tenderizer";

    public string BaseUrl { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpUser { get; set; }
    public string? SmtpPass { get; set; }
}
