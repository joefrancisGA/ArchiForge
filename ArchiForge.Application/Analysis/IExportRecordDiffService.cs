using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Application.Analysis;

public interface IExportRecordDiffService
{
    ExportRecordDiffResult Compare(
        RunExportRecord left,
        RunExportRecord right);
}

