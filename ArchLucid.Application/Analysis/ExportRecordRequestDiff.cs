namespace ArchLucid.Application.Analysis;

public sealed class ExportRecordRequestDiff
{
    public List<string> ChangedFlags
    {
        get;
        set;
    } = [];

    public List<string> ChangedValues
    {
        get;
        set;
    } = [];

    public PersistedAnalysisExportRequest? LeftRequest
    {
        get;
        set;
    }

    public PersistedAnalysisExportRequest? RightRequest
    {
        get;
        set;
    }
}
