using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

public interface IExportRecordDiffService
{
    ExportRecordDiffResult Compare(
        RunExportRecord left,
        RunExportRecord right);
}
