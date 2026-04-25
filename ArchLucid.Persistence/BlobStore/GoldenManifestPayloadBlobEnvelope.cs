using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Persistence.GoldenManifests;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>Single JSON blob mirroring <see cref="GoldenManifestStorageRow" /> JSON columns for offload.</summary>
public sealed class GoldenManifestPayloadBlobEnvelope
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion
    {
        get;
        init;
    } = CurrentSchemaVersion;

    [JsonPropertyName("metadataJson")]
    public string MetadataJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("requirementsJson")]
    public string RequirementsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("topologyJson")]
    public string TopologyJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("securityJson")]
    public string SecurityJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("complianceJson")]
    public string? ComplianceJson
    {
        get;
        init;
    }

    [JsonPropertyName("costJson")]
    public string CostJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("constraintsJson")]
    public string ConstraintsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("unresolvedIssuesJson")]
    public string UnresolvedIssuesJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("decisionsJson")]
    public string DecisionsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("assumptionsJson")]
    public string AssumptionsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("warningsJson")]
    public string WarningsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("provenanceJson")]
    public string ProvenanceJson
    {
        get;
        init;
    } = "";

    public static GoldenManifestPayloadBlobEnvelope FromSerializedSlices(
        string metadataJson,
        string requirementsJson,
        string topologyJson,
        string securityJson,
        string? complianceJson,
        string costJson,
        string constraintsJson,
        string unresolvedIssuesJson,
        string decisionsJson,
        string assumptionsJson,
        string warningsJson,
        string provenanceJson)
    {
        return new GoldenManifestPayloadBlobEnvelope
        {
            SchemaVersion = CurrentSchemaVersion,
            MetadataJson = metadataJson,
            RequirementsJson = requirementsJson,
            TopologyJson = topologyJson,
            SecurityJson = securityJson,
            ComplianceJson = complianceJson,
            CostJson = costJson,
            ConstraintsJson = constraintsJson,
            UnresolvedIssuesJson = unresolvedIssuesJson,
            DecisionsJson = decisionsJson,
            AssumptionsJson = assumptionsJson,
            WarningsJson = warningsJson,
            ProvenanceJson = provenanceJson
        };
    }

    public static int SumUtf16Length(
        string metadataJson,
        string requirementsJson,
        string topologyJson,
        string securityJson,
        string? complianceJson,
        string costJson,
        string constraintsJson,
        string unresolvedIssuesJson,
        string decisionsJson,
        string assumptionsJson,
        string warningsJson,
        string provenanceJson)
    {
        return metadataJson.Length
               + requirementsJson.Length
               + topologyJson.Length
               + securityJson.Length
               + (complianceJson?.Length ?? 0)
               + costJson.Length
               + constraintsJson.Length
               + unresolvedIssuesJson.Length
               + decisionsJson.Length
               + assumptionsJson.Length
               + warningsJson.Length
               + provenanceJson.Length;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    public static GoldenManifestPayloadBlobEnvelope? TryDeserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<GoldenManifestPayloadBlobEnvelope>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Replaces JSON columns on the row when a blob overlay is applied; scalars unchanged.</summary>
    internal static GoldenManifestStorageRow MergeIntoRow(GoldenManifestStorageRow row,
        GoldenManifestPayloadBlobEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(envelope);

        return new GoldenManifestStorageRow
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            ManifestId = row.ManifestId,
            RunId = row.RunId,
            ContextSnapshotId = row.ContextSnapshotId,
            GraphSnapshotId = row.GraphSnapshotId,
            FindingsSnapshotId = row.FindingsSnapshotId,
            DecisionTraceId = row.DecisionTraceId,
            CreatedUtc = row.CreatedUtc,
            ManifestHash = row.ManifestHash,
            RuleSetId = row.RuleSetId,
            RuleSetVersion = row.RuleSetVersion,
            RuleSetHash = row.RuleSetHash,
            MetadataJson = envelope.MetadataJson,
            RequirementsJson = envelope.RequirementsJson,
            TopologyJson = envelope.TopologyJson,
            SecurityJson = envelope.SecurityJson,
            ComplianceJson = envelope.ComplianceJson ?? string.Empty,
            CostJson = envelope.CostJson,
            ConstraintsJson = envelope.ConstraintsJson,
            UnresolvedIssuesJson = envelope.UnresolvedIssuesJson,
            DecisionsJson = envelope.DecisionsJson,
            AssumptionsJson = envelope.AssumptionsJson,
            WarningsJson = envelope.WarningsJson,
            ProvenanceJson = envelope.ProvenanceJson,
            ManifestPayloadBlobUri = row.ManifestPayloadBlobUri
        };
    }
}
