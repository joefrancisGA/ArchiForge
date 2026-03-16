namespace ArchiForge.Contracts.Metadata;

public sealed class RunExportRecord
{
    public string ExportRecordId { get; set; } = Guid.NewGuid().ToString("N");

    public string RunId { get; set; } = string.Empty;

    public string ExportType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? TemplateProfile { get; set; }

    public string? TemplateProfileDisplayName { get; set; }

    public bool WasAutoSelected { get; set; }

    public string? ResolutionReason { get; set; }

    public string? ManifestVersion { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

