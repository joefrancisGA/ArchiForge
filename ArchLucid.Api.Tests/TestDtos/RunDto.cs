namespace ArchLucid.Api.Tests.TestDtos;

public sealed class RunDto
{
    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string RequestId
    {
        get;
        set;
    } = string.Empty;

    public string Status
    {
        get;
        set;
    } = string.Empty;

    public string? CurrentManifestVersion
    {
        get;
        set;
    }
}
