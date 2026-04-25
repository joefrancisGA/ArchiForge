using System.Text.Json.Serialization;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>
///     Cosmos document for <see cref="ArchLucid.KnowledgeGraph.Models.GraphSnapshot" /> (partition:
///     <c>/graphSnapshotId</c>).
/// </summary>
public sealed class GraphSnapshotDocument
{
    [JsonPropertyName("id")]
    public string Id
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("graphSnapshotId")]
    public string GraphSnapshotId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("contextSnapshotId")]
    public string ContextSnapshotId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("runId")]
    public string RunId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion
    {
        get;
        set;
    }

    /// <summary>ISO 8601 UTC string for ordering in cross-partition queries.</summary>
    [JsonPropertyName("createdUtc")]
    public string CreatedUtc
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("nodesJson")]
    public string NodesJson
    {
        get;
        set;
    } = "[]";

    [JsonPropertyName("edgesJson")]
    public string EdgesJson
    {
        get;
        set;
    } = "[]";

    [JsonPropertyName("warningsJson")]
    public string WarningsJson
    {
        get;
        set;
    } = "[]";
}
