namespace ArchLucid.Api.Tests;

public sealed class AgentEvidencePackageResponse
{
    public AgentEvidencePackageDto Evidence
    {
        get;
        set;
    } = new();
}

public sealed class AgentEvidencePackageDto
{
    public string EvidencePackageId
    {
        get;
        set;
    } = string.Empty;

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

    public string SystemName
    {
        get;
        set;
    } = string.Empty;

    public string Environment
    {
        get;
        set;
    } = string.Empty;

    public string CloudProvider
    {
        get;
        set;
    } = string.Empty;

    public RequestEvidenceDto Request
    {
        get;
        set;
    } = new();

    public List<PolicyEvidenceDto> Policies
    {
        get;
        set;
    } = [];

    public List<ServiceCatalogEvidenceDto> ServiceCatalog
    {
        get;
        set;
    } = [];
}

public sealed class RequestEvidenceDto
{
    public string Description
    {
        get;
        set;
    } = string.Empty;

    public List<string> Constraints
    {
        get;
        set;
    } = [];

    public List<string> RequiredCapabilities
    {
        get;
        set;
    } = [];

    public List<string> Assumptions
    {
        get;
        set;
    } = [];
}

public sealed class PolicyEvidenceDto
{
    public string PolicyId
    {
        get;
        set;
    } = string.Empty;

    public string Title
    {
        get;
        set;
    } = string.Empty;

    public string Summary
    {
        get;
        set;
    } = string.Empty;

    public List<string> RequiredControls
    {
        get;
        set;
    } = [];

    public List<string> Tags
    {
        get;
        set;
    } = [];
}

public sealed class ServiceCatalogEvidenceDto
{
    public string ServiceId
    {
        get;
        set;
    } = string.Empty;

    public string ServiceName
    {
        get;
        set;
    } = string.Empty;

    public string Category
    {
        get;
        set;
    } = string.Empty;

    public string Summary
    {
        get;
        set;
    } = string.Empty;

    public List<string> Tags
    {
        get;
        set;
    } = [];

    public List<string> RecommendedUseCases
    {
        get;
        set;
    } = [];
}
