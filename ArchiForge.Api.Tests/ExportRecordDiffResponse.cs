namespace ArchiForge.Api.Tests;

public sealed class ExportRecordDiffResponse
{
    public ExportRecordDiffDto Diff { get; set; } = new();
}

public sealed class ExportRecordDiffDto
{
    public string LeftExportRecordId { get; set; } = string.Empty;
    public string RightExportRecordId { get; set; } = string.Empty;
    public string LeftRunId { get; set; } = string.Empty;
    public string RightRunId { get; set; } = string.Empty;
    public List<string> ChangedTopLevelFields { get; set; } = [];
    public ExportRecordRequestDiffDto RequestDiff { get; set; } = new();
    public List<string> Warnings { get; set; } = [];
}

public sealed class ExportRecordRequestDiffDto
{
    public List<string> ChangedFlags { get; set; } = [];
    public List<string> ChangedValues { get; set; } = [];
}

