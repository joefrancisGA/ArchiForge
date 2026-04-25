using System.Text.Json.Serialization;

namespace ArchLucid.Persistence.Cosmos;

/// <summary>Cosmos document for agent execution traces (partition: <c>/runId</c>).</summary>
public sealed class AgentTraceDocument
{
    [JsonPropertyName("id")]
    public string Id
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

    [JsonPropertyName("taskId")]
    public string TaskId
    {
        get;
        set;
    } = string.Empty;

    [JsonPropertyName("createdUtc")]
    public string CreatedUtc
    {
        get;
        set;
    } = string.Empty;

    /// <summary>Serialized <see cref="ArchLucid.Contracts.Agents.AgentExecutionTrace" /> (same shape as SQL <c>TraceJson</c>).</summary>
    [JsonPropertyName("traceJson")]
    public string TraceJson
    {
        get;
        set;
    } = "{}";

    /// <summary>Optional Cosmos TTL in seconds (container default may also apply).</summary>
    [JsonPropertyName("ttl")]
    public int? Ttl
    {
        get;
        set;
    }
}
