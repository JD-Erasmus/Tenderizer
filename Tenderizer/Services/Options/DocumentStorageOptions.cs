namespace Tenderizer.Services.Options;

public sealed class DocumentStorageOptions
{
    public string PrivateRootFolder { get; set; } = "App_Data/Documents";
    public int MaxFileSizeMb { get; set; } = 25;
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
    ];
}
