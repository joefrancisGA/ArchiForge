namespace ArchiForge.Api.Models;

public sealed class ManifestCompareExportResponse
{
    public string LeftManifestVersion { get; set; } = string.Empty;

    public string RightManifestVersion { get; set; } = string.Empty;

    public string Format { get; set; } = "markdown";

    public string FileName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
