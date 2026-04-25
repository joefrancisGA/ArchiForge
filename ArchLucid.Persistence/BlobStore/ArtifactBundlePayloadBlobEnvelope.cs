using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Persistence.ArtifactBundles;

namespace ArchLucid.Persistence.BlobStore;

/// <summary>Combined artifacts + trace JSON for a single bundle blob.</summary>
public sealed class ArtifactBundlePayloadBlobEnvelope
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion
    {
        get;
        init;
    } = CurrentSchemaVersion;

    [JsonPropertyName("artifactsJson")]
    public string ArtifactsJson
    {
        get;
        init;
    } = "";

    [JsonPropertyName("traceJson")]
    public string TraceJson
    {
        get;
        init;
    } = "";

    public static ArtifactBundlePayloadBlobEnvelope FromJsonPair(string artifactsJson, string traceJson)
    {
        return new ArtifactBundlePayloadBlobEnvelope
        {
            SchemaVersion = CurrentSchemaVersion, ArtifactsJson = artifactsJson, TraceJson = traceJson
        };
    }

    public static int SumUtf16Length(string artifactsJson, string traceJson)
    {
        return artifactsJson.Length + traceJson.Length;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    public static ArtifactBundlePayloadBlobEnvelope? TryDeserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ArtifactBundlePayloadBlobEnvelope>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static ArtifactBundleStorageRow MergeIntoRow(ArtifactBundleStorageRow row,
        ArtifactBundlePayloadBlobEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(envelope);

        return new ArtifactBundleStorageRow
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            BundleId = row.BundleId,
            RunId = row.RunId,
            ManifestId = row.ManifestId,
            CreatedUtc = row.CreatedUtc,
            ArtifactsJson = envelope.ArtifactsJson,
            TraceJson = envelope.TraceJson,
            BundlePayloadBlobUri = row.BundlePayloadBlobUri
        };
    }
}
