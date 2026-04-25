namespace ArchLucid.Api.Tests;

public sealed class ManifestDiffDto
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

    public List<object> AddedRelationships
    {
        get;
        set;
    } = [];

    public List<object> RemovedRelationships
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
