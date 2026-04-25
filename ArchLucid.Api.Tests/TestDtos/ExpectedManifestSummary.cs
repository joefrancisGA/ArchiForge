namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ExpectedManifestSummary
{
    public string SystemName
    {
        get;
        set;
    } = string.Empty;

    public List<string> Services
    {
        get;
        set;
    } = [];

    public List<string> Datastores
    {
        get;
        set;
    } = [];

    public List<string> RequiredControls
    {
        get;
        set;
    } = [];
}
