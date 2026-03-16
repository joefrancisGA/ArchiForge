namespace ArchiForge.Api.Tests;

public sealed class RunExportHistoryResponse
{
    public List<RunExportRecordDto> Exports { get; set; } = [];
}

public sealed class RunExportRecordDto
{
    public string ExportRecordId { get; set; } = string.Empty;
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
}

