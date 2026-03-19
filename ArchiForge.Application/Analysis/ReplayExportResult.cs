namespace ArchiForge.Application.Analysis;

public sealed class ReplayExportResult
{
    public string ExportRecordId { get; set; } = string.Empty;

    public string RunId { get; set; } = string.Empty;

    public string ExportType { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];

    public string? TemplateProfile { get; set; }

    public string? TemplateProfileDisplayName { get; set; }

    public bool WasAutoSelected { get; set; }

    public string? ResolutionReason { get; set; }
}

