namespace ArchiForge.Application.Analysis;

public sealed class ReplayExportRequest
{
    public string ExportRecordId { get; set; } = string.Empty;
    public bool RecordReplayExport { get; set; } = false;
}

