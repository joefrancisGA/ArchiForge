namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ManifestGovernanceDto
{
    public List<string> RequiredControls
    {
        get;
        set;
    } = [];

    public List<string> ComplianceTags
    {
        get;
        set;
    } = [];
}
