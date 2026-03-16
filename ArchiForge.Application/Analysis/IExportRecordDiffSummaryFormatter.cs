namespace ArchiForge.Application.Analysis;

public interface IExportRecordDiffSummaryFormatter
{
    string FormatMarkdown(ExportRecordDiffResult diff);
}

