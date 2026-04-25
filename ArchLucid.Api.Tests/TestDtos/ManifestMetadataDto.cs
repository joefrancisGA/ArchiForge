namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ManifestMetadataDto
{
    public string ManifestVersion
    {
        get;
        set;
    } = string.Empty;

    public string? ParentManifestVersion
    {
        get;
        set;
    }

    public List<string> DecisionTraceIds
    {
        get;
        set;
    } = [];
}
