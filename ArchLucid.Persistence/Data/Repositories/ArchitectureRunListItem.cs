namespace ArchLucid.Persistence.Data.Repositories;

public sealed class ArchitectureRunListItem
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

    public DateTime CreatedUtc
    {
        get;
        set;
    }

    public DateTime? CompletedUtc
    {
        get;
        set;
    }

    public string? CurrentManifestVersion
    {
        get;
        set;
    }

    public string SystemName
    {
        get;
        set;
    } = string.Empty;
}
