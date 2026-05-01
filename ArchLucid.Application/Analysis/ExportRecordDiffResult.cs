namespace ArchLucid.Application.Analysis;

public sealed class ExportRecordDiffResult
{
    public string LeftExportRecordId
    {
        get;
        set;
    } = string.Empty;

    public string RightExportRecordId
    {
        get;
        set;
    } = string.Empty;

    public string LeftRunId
    {
        get;
        set;
    } = string.Empty;

    public string RightRunId
    {
        get;
        set;
    } = string.Empty;

    public List<string> ChangedTopLevelFields
    {
        get;
        set;
    } = [];

    public ExportRecordRequestDiff RequestDiff
    {
        get;
        set;
    } = new();

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
