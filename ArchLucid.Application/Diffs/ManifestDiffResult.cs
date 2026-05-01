namespace ArchLucid.Application.Diffs;

public sealed class ManifestDiffResult
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

    public List<string> AddedServices
    {
        get;
        set;
    } = [];

    public List<string> RemovedServices
    {
        get;
        set;
    } = [];

    public List<string> AddedDatastores
    {
        get;
        set;
    } = [];

    public List<string> RemovedDatastores
    {
        get;
        set;
    } = [];

    public List<string> AddedRequiredControls
    {
        get;
        set;
    } = [];

    public List<string> RemovedRequiredControls
    {
        get;
        set;
    } = [];

    public List<RelationshipDiffItem> AddedRelationships
    {
        get;
        set;
    } = [];

    public List<RelationshipDiffItem> RemovedRelationships
    {
        get;
        set;
    } = [];

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
