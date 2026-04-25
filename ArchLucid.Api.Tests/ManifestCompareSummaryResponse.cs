namespace ArchLucid.Api.Tests;

public sealed class ManifestCompareSummaryResponse
{
    public string LeftManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string RightManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string Format
    {
        get;
        set;
    } = string.Empty;

    public string Summary
    {
        get;
        set;
    } = string.Empty;

    public ManifestDiffDto Diff
    {
        get;
        set;
    } = new();
}
