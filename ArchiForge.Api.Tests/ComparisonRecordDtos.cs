namespace ArchiForge.Api.Tests;

public sealed class ComparisonRecordResponseDto
{
    public ComparisonRecordDto Record { get; set; } = new();
}

public sealed class ComparisonHistoryResponseDto
{
    public List<ComparisonRecordDto> Records { get; set; } = [];
}

public sealed class ComparisonRecordDto
{
    public string ComparisonRecordId { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public string? LeftRunId { get; set; }
    public string? RightRunId { get; set; }
    public string? LeftManifestVersion { get; set; }
    public string? RightManifestVersion { get; set; }
    public string? LeftExportRecordId { get; set; }
    public string? RightExportRecordId { get; set; }
    public string Format { get; set; } = string.Empty;
    public string? SummaryMarkdown { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

