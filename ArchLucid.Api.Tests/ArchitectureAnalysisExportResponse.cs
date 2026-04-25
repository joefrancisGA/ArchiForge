namespace ArchLucid.Api.Tests;

public sealed class ArchitectureAnalysisExportResponse
{
    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string Format
    {
        get;
        set;
    } = string.Empty;

    public string FileName
    {
        get;
        set;
    } = string.Empty;

    public string Content
    {
        get;
        set;
    } = string.Empty;
}
