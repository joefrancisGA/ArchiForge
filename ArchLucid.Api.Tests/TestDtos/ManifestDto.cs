namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ManifestDto
{
    public string RunId
    {
        get;
        set;
    } = string.Empty;

    public string SystemName
    {
        get;
        set;
    } = string.Empty;

    public List<ManifestServiceDto> Services
    {
        get;
        set;
    } = [];

    public List<ManifestDatastoreDto> Datastores
    {
        get;
        set;
    } = [];

    public ManifestGovernanceDto Governance
    {
        get;
        set;
    } = new();

    public ManifestMetadataDto Metadata
    {
        get;
        set;
    } = new();
}
