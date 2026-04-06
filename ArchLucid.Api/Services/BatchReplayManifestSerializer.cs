using System.Text.Json;

using ArchiForge.Api.Models;

namespace ArchiForge.Api.Services;

/// <summary>Serializes <see cref="BatchReplayManifestDocument"/> for inclusion in batch replay ZIP files.</summary>
public static class BatchReplayManifestSerializer
{
    public const string ManifestEntryName = "batch-replay-manifest.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static byte[] ToUtf8Bytes(BatchReplayManifestDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
    }
}
