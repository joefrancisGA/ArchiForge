namespace ArchLucid.Api.Tests.TestDtos;

public sealed class ManifestDatastoreDto
{
    public string DatastoreId
    {
        get;
        set;
    } = string.Empty;

    public string DatastoreName
    {
        get;
        set;
    } = string.Empty;

    public bool PrivateEndpointRequired
    {
        get;
        set;
    }
}
