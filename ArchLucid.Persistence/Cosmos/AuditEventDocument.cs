using System.Text.Json.Serialization;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Cosmos document for <see cref="ArchLucid.Core.Audit.AuditEvent" /> (partition: <c>/tenantId</c>).</summary>
public sealed class AuditEventDocument
{
    [JsonPropertyName("id")]
    public string Id
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("tenantId")]
    public string TenantId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string WorkspaceId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("projectId")]
    public string ProjectId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("occurredUtc")]
    public string OccurredUtc
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("eventType")]
    public string EventType
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("actorUserId")]
    public string ActorUserId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("actorUserName")]
    public string ActorUserName
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("runId")]
    public string? RunId
    {
        get;
        set;
    }

    [JsonPropertyName("manifestId")]
    public string? ManifestId
    {
        get;
        set;
    }

    [JsonPropertyName("artifactId")]
    public string? ArtifactId
    {
        get;
        set;
    }

    [JsonPropertyName("dataJson")]
    public string DataJson
    {
        get;
        set;
    } = "{}";

    [JsonPropertyName("correlationId")]
    public string? CorrelationId
    {
        get;
        set;
    }
}
